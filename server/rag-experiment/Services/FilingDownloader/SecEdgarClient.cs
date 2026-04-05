using System.Text.Json;
using Microsoft.Extensions.Options;
using rag_experiment.Services.BackgroundJobs.Models;
using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// SEC EDGAR client for downloading company filings.
/// Implements rate limiting (10 requests/second) as required by SEC EDGAR fair access policy.
/// </summary>
public class SecEdgarClient : IFilingDownloader, ICompanyFilingsService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly int _maxFilingsToDownload;

    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(100);

    private const string EdgarBaseUrl = "https://www.sec.gov";
    private const string EdgarDataUrl = "https://data.sec.gov";

    public SecEdgarClient(HttpClient httpClient, IOptions<FilingIngestionOptions> filingOptions)
    {
        _httpClient = httpClient;
        _maxFilingsToDownload = filingOptions.Value.MaxFilingsToDownload;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DueDiligence/1.0 (dorin.rogov@gmail.com)");
    }

    public async Task<List<FilingDocument>> DownloadFilingsAsync(
        string companyIdentifier,
        List<string> filingTypes,
        CancellationToken ct = default)
    {
        var documents = new List<FilingDocument>();
        var cik = await ResolveCompanyIdentifierToCikAsync(companyIdentifier, ct);
        if (string.IsNullOrEmpty(cik))
        {
            return documents;
        }

        var filings = await GetCompanyFilingsAsync(cik, filingTypes, _maxFilingsToDownload, ct);
        foreach (var filing in filings)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var document = await DownloadSingleFilingAsync(cik, filing, ct);
                if (document != null)
                {
                    documents.Add(document);
                }
            }
            catch (Exception)
            {
                // Continue with remaining filings if one fails
            }
        }

        return documents;
    }

    public async Task<SecCompanyFilingsLookup?> GetAvailableFilingsAsync(
        string companyIdentifier,
        CancellationToken ct = default)
    {
        var cik = await ResolveCompanyIdentifierToCikAsync(companyIdentifier, ct);
        if (string.IsNullOrEmpty(cik))
        {
            return null;
        }

        using var doc = await GetCompanySubmissionsAsync(cik, ct);
        if (doc == null)
        {
            return null;
        }

        return new SecCompanyFilingsLookup
        {
            Cik = cik,
            Name = doc.RootElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null,
            Tickers = GetStringArray(doc.RootElement, "tickers"),
            Exchanges = GetStringArray(doc.RootElement, "exchanges"),
            AvailableFilingTypes = GetAvailableFilingTypes(doc.RootElement)
        };
    }

    private async Task<string?> ResolveCompanyIdentifierToCikAsync(
        string companyIdentifier,
        CancellationToken ct)
    {
        if (long.TryParse(companyIdentifier, out var numericIdentifier))
        {
            return numericIdentifier.ToString("D10");
        }

        await EnforceRateLimitAsync(ct);
        var tickersUrl = $"{EdgarBaseUrl}/files/company_tickers.json";

        try
        {
            var response = await _httpClient.GetAsync(tickersUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            foreach (var entry in doc.RootElement.EnumerateObject())
            {
                if (entry.Value.TryGetProperty("ticker", out var tickerElement) &&
                    string.Equals(tickerElement.GetString(), companyIdentifier, StringComparison.OrdinalIgnoreCase) &&
                    entry.Value.TryGetProperty("cik_str", out var cikStrElement))
                {
                    var cik = cikStrElement.GetInt64();
                    return cik.ToString("D10");
                }
            }
        }
        catch (Exception)
        {
            // Failed to look up CIK
        }

        return null;
    }

    private async Task<JsonDocument?> GetCompanySubmissionsAsync(string cik, CancellationToken ct)
    {
        await EnforceRateLimitAsync(ct);
        var submissionsUrl = $"{EdgarDataUrl}/submissions/CIK{cik}.json";

        try
        {
            var response = await _httpClient.GetAsync(submissionsUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonDocument.Parse(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<List<FilingInfo>> GetCompanyFilingsAsync(
        string cik,
        List<string> filingTypes,
        int maxFilings,
        CancellationToken ct)
    {
        var filings = new List<FilingInfo>();
        using var doc = await GetCompanySubmissionsAsync(cik, ct);
        if (doc == null)
        {
            return filings;
        }

        try
        {
            if (!doc.RootElement.TryGetProperty("filings", out var filingsElement) ||
                !filingsElement.TryGetProperty("recent", out var recentElement))
            {
                return filings;
            }

            var forms = recentElement.GetProperty("form").EnumerateArray().ToList();
            var accessionNumbers = recentElement.GetProperty("accessionNumber").EnumerateArray().ToList();
            var filingDates = recentElement.GetProperty("filingDate").EnumerateArray().ToList();
            var primaryDocuments = recentElement.GetProperty("primaryDocument").EnumerateArray().ToList();

            for (var i = 0; i < forms.Count; i++)
            {
                var form = forms[i].GetString();
                if (form == null || !filingTypes.Contains(form, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var accessionNumber = accessionNumbers[i].GetString() ?? string.Empty;
                var filingDateStr = filingDates[i].GetString();
                var primaryDoc = primaryDocuments[i].GetString() ?? string.Empty;

                if (!DateOnly.TryParse(filingDateStr, out var filingDate))
                {
                    continue;
                }

                filings.Add(new FilingInfo
                {
                    Form = form,
                    AccessionNumber = accessionNumber,
                    FilingDate = filingDate,
                    PrimaryDocument = primaryDoc
                });

                if (maxFilings > 0 && filings.Count >= maxFilings)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Failed to get company filings
        }

        return filings;
    }

    private async Task<FilingDocument?> DownloadSingleFilingAsync(
        string cik,
        FilingInfo filing,
        CancellationToken ct)
    {
        await EnforceRateLimitAsync(ct);

        var accessionForUrl = filing.AccessionNumber.Replace("-", "");
        var documentUrl =
            $"{EdgarBaseUrl}/Archives/edgar/data/{cik.TrimStart('0')}/{accessionForUrl}/{filing.PrimaryDocument}";

        var response = await _httpClient.GetAsync(documentUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var safeForm = SanitizeFileNamePart(filing.Form);
        var fileName = $"{safeForm}_{filing.AccessionNumber}{Path.GetExtension(filing.PrimaryDocument)}";

        return new FilingDocument
        {
            Content = content,
            FileName = fileName,
            FilingType = filing.Form,
            AccessionNumber = filing.AccessionNumber,
            FilingDate = filing.FilingDate,
            CompanyIdentifier = cik
        };
    }

    private static IReadOnlyList<SecAvailableFilingType> GetAvailableFilingTypes(JsonElement root)
    {
        if (!root.TryGetProperty("filings", out var filingsElement) ||
            !filingsElement.TryGetProperty("recent", out var recentElement) ||
            !recentElement.TryGetProperty("form", out var formsElement))
        {
            return Array.Empty<SecAvailableFilingType>();
        }

        var forms = formsElement.EnumerateArray().ToList();
        var filingDates = recentElement.TryGetProperty("filingDate", out var datesElement)
            ? datesElement.EnumerateArray().ToList()
            : new List<JsonElement>();

        var summaries = new Dictionary<string, SecAvailableFilingType>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < forms.Count; i++)
        {
            var form = forms[i].GetString();
            if (string.IsNullOrWhiteSpace(form))
            {
                continue;
            }

            summaries.TryGetValue(form, out var existing);
            var filingCount = (existing?.FilingCount ?? 0) + 1;
            var latestFilingDate = existing?.LatestFilingDate;

            if (i < filingDates.Count &&
                DateOnly.TryParse(filingDates[i].GetString(), out var parsedDate) &&
                (!latestFilingDate.HasValue || parsedDate > latestFilingDate.Value))
            {
                latestFilingDate = parsedDate;
            }

            summaries[form] = new SecAvailableFilingType
            {
                FormType = form,
                FilingCount = filingCount,
                LatestFilingDate = latestFilingDate
            };
        }

        return summaries.Values
            .OrderBy(form => form.FormType, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property.EnumerateArray()
            .Select(element => element.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList()
            .AsReadOnly();
    }

    private static string SanitizeFileNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }).ToHashSet();
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < MinRequestInterval)
            {
                await Task.Delay(MinRequestInterval - timeSinceLastRequest, ct);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private record FilingInfo
    {
        public required string Form { get; init; }
        public required string AccessionNumber { get; init; }
        public required DateOnly FilingDate { get; init; }
        public required string PrimaryDocument { get; init; }
    }
}
