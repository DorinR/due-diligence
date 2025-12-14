using System.Text.Json;
using Microsoft.Extensions.Options;
using rag_experiment.Services.BackgroundJobs.Models;
using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// SEC EDGAR client for downloading company filings.
/// Implements rate limiting (10 requests/second) as required by SEC EDGAR fair access policy.
/// </summary>
public class SecEdgarClient : IFilingDownloader
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly int _maxFilingsToDownload;

    // SEC EDGAR requires 100ms minimum between requests (10 req/sec)
    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(100);

    // SEC EDGAR base URLs
    private const string EdgarBaseUrl = "https://www.sec.gov";
    private const string EdgarDataUrl = "https://data.sec.gov";

    public SecEdgarClient(HttpClient httpClient, IOptions<FilingIngestionOptions> filingOptions)
    {
        _httpClient = httpClient;
        _maxFilingsToDownload = filingOptions.Value.MaxFilingsToDownload;

        // SEC EDGAR requires a User-Agent header with contact info
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DueDiligence/1.0 (dorin.rogov@gmail.com)");
    }

    /// <inheritdoc />
    public async Task<List<FilingDocument>> DownloadFilingsAsync(
        string companyIdentifier,
        List<string> filingTypes,
        CancellationToken ct = default)
    {
        var documents = new List<FilingDocument>();

        // First, get the company's CIK (Central Index Key) if a ticker was provided
        var cik = await GetCikAsync(companyIdentifier, ct);
        if (string.IsNullOrEmpty(cik))
        {
            return documents;
        }

        // Get the company's filing history
        var filings = await GetCompanyFilingsAsync(cik, filingTypes, _maxFilingsToDownload, ct);

        // Download each filing
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

    /// <summary>
    /// Resolves a company ticker to its SEC CIK number.
    /// </summary>
    private async Task<string?> GetCikAsync(string companyIdentifier, CancellationToken ct)
    {
        await EnforceRateLimitAsync(ct);
        // SEC hosts the ticker -> CIK map under sec.gov/files (not data.sec.gov)
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
                    string.Equals(tickerElement.GetString(), companyIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Value.TryGetProperty("cik_str", out var cikStrElement))
                    {
                        var cik = cikStrElement.GetInt64();
                        return cik.ToString("D10");
                    }
                }
            }
        }
        catch (Exception)
        {
            // Failed to look up CIK
        }

        return null;
    }

    /// <summary>
    /// Gets the list of filings for a company, filtered by type.
    /// </summary>
    private async Task<List<FilingInfo>> GetCompanyFilingsAsync(
        string cik,
        List<string> filingTypes,
        int maxFilings,
        CancellationToken ct)
    {
        var filings = new List<FilingInfo>();

        await EnforceRateLimitAsync(ct);
        var submissionsUrl = $"{EdgarDataUrl}/submissions/CIK{cik}.json";

        try
        {
            var response = await _httpClient.GetAsync(submissionsUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("filings", out var filingsElement) ||
                !filingsElement.TryGetProperty("recent", out var recentElement))
            {
                return filings;
            }

            // Extract arrays from the recent filings
            var forms = recentElement.GetProperty("form").EnumerateArray().ToList();
            var accessionNumbers = recentElement.GetProperty("accessionNumber").EnumerateArray().ToList();
            var filingDates = recentElement.GetProperty("filingDate").EnumerateArray().ToList();
            var primaryDocuments = recentElement.GetProperty("primaryDocument").EnumerateArray().ToList();

            for (int i = 0; i < forms.Count; i++)
            {
                var form = forms[i].GetString();
                if (form != null && filingTypes.Contains(form, StringComparer.OrdinalIgnoreCase))
                {
                    var accessionNumber = accessionNumbers[i].GetString() ?? "";
                    var filingDateStr = filingDates[i].GetString();
                    var primaryDoc = primaryDocuments[i].GetString() ?? "";

                    if (DateOnly.TryParse(filingDateStr, out var filingDate))
                    {
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
            }
        }
        catch (Exception)
        {
            // Failed to get company filings
        }

        return filings;
    }

    /// <summary>
    /// Downloads a single filing document.
    /// </summary>
    private async Task<FilingDocument?> DownloadSingleFilingAsync(
        string cik,
        FilingInfo filing,
        CancellationToken ct)
    {
        await EnforceRateLimitAsync(ct);

        // Format accession number for URL (remove dashes)
        var accessionForUrl = filing.AccessionNumber.Replace("-", "");
        var documentUrl =
            $"{EdgarBaseUrl}/Archives/edgar/data/{cik.TrimStart('0')}/{accessionForUrl}/{filing.PrimaryDocument}";

        var response = await _httpClient.GetAsync(documentUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = $"{filing.Form}_{filing.AccessionNumber}{Path.GetExtension(filing.PrimaryDocument)}";

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

    /// <summary>
    /// Enforces SEC EDGAR rate limit (10 requests per second).
    /// </summary>
    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < MinRequestInterval)
            {
                var delay = MinRequestInterval - timeSinceLastRequest;
                await Task.Delay(delay, ct);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    /// <summary>
    /// Internal record for tracking filing metadata during download.
    /// </summary>
    private record FilingInfo
    {
        public required string Form { get; init; }
        public required string AccessionNumber { get; init; }
        public required DateOnly FilingDate { get; init; }
        public required string PrimaryDocument { get; init; }
    }
}