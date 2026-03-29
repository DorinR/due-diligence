using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Provides access to the SEC company directory feed.
/// </summary>
public interface ICompanyDirectoryService
{
    Task<IReadOnlyList<SecCompanyInfo>> GetCompaniesAsync(CancellationToken ct = default);
}
