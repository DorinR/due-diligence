using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Interface for downloading SEC filings from an external data source.
/// Implementations handle the specifics of interacting with the data provider API
/// (e.g., SEC EDGAR) including rate limiting and retry logic.
/// </summary>
public interface IFilingDownloader
{
    /// <summary>
    /// Downloads filings for a specified company from the data source.
    /// </summary>
    /// <param name="companyIdentifier">The company ticker symbol or CIK number.</param>
    /// <param name="filingTypes">List of filing types to download (e.g., ["10-K", "10-Q", "8-K"]).</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>A list of downloaded filing documents with their content and metadata.</returns>
    Task<List<FilingDocument>> DownloadFilingsAsync(
        string companyIdentifier,
        List<string> filingTypes,
        CancellationToken ct = default);
}