using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Reads the SEC company directory feed and caches the filtered result.
/// </summary>
public class SecCompanyDirectoryClient : ICompanyDirectoryService
{
    private const string EdgarBaseUrl = "https://www.sec.gov";
    private const string CompanyDirectoryUrl = $"{EdgarBaseUrl}/files/company_tickers_exchange.json";
    private const string CacheKey = "sec-company-directory";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public SecCompanyDirectoryClient(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DueDiligence/1.0 (dorin.rogov@gmail.com)");
    }

    public async Task<IReadOnlyList<SecCompanyInfo>> GetCompaniesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<IReadOnlyList<SecCompanyInfo>>(CacheKey, out var cachedCompanies) &&
            cachedCompanies != null)
        {
            return cachedCompanies;
        }

        var response = await _httpClient.GetAsync(CompanyDirectoryUrl, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var fields = doc.RootElement.GetProperty("fields")
            .EnumerateArray()
            .Select(field => field.GetString() ?? string.Empty)
            .ToList();
        var fieldIndex = fields
            .Select((field, index) => new { field, index })
            .ToDictionary(x => x.field, x => x.index, StringComparer.OrdinalIgnoreCase);

        var companies = doc.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(entry => MapCompany(entry, fieldIndex))
            .Where(company =>
                !string.IsNullOrWhiteSpace(company.Name) &&
                !string.IsNullOrWhiteSpace(company.Ticker) &&
                Fortune500CompanyFilter.IsIncluded(company.Name))
            .OrderBy(company => company.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        _cache.Set(CacheKey, companies, CacheDuration);

        return companies;
    }

    private static SecCompanyInfo MapCompany(
        JsonElement entry,
        IReadOnlyDictionary<string, int> fieldIndex)
    {
        var cikNumber = GetLong(entry, fieldIndex, "cik");

        return new SecCompanyInfo
        {
            CikNumber = cikNumber,
            Cik = cikNumber.ToString("D10"),
            Name = GetString(entry, fieldIndex, "name"),
            Ticker = GetOptionalString(entry, fieldIndex, "ticker"),
            Exchange = GetOptionalString(entry, fieldIndex, "exchange")
        };
    }

    private static long GetLong(JsonElement entry, IReadOnlyDictionary<string, int> fieldIndex, string fieldName)
    {
        if (!fieldIndex.TryGetValue(fieldName, out var index) || index >= entry.GetArrayLength())
        {
            return 0;
        }

        var value = entry[index];
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        return long.TryParse(value.GetString(), out var parsed) ? parsed : 0;
    }

    private static string GetString(JsonElement entry, IReadOnlyDictionary<string, int> fieldIndex, string fieldName)
    {
        return GetOptionalString(entry, fieldIndex, fieldName) ?? string.Empty;
    }

    private static string? GetOptionalString(JsonElement entry, IReadOnlyDictionary<string, int> fieldIndex, string fieldName)
    {
        if (!fieldIndex.TryGetValue(fieldName, out var index) || index >= entry.GetArrayLength())
        {
            return null;
        }

        var value = entry[index];
        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }
}
