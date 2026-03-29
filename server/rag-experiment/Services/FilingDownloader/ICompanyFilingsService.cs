using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Provides filing availability for a specific SEC company.
/// </summary>
public interface ICompanyFilingsService
{
    Task<SecCompanyFilingsLookup?> GetAvailableFilingsAsync(
        string companyIdentifier,
        CancellationToken ct = default);
}
