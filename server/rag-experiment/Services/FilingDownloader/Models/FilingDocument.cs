namespace rag_experiment.Services.FilingDownloader.Models;

/// <summary>
/// Represents a downloaded SEC filing document with its content and metadata.
/// Used to pass filing data between the download and persistence stages of the ingestion pipeline.
/// </summary>
public class FilingDocument
{
    /// <summary>
    /// The raw content of the filing document as bytes.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// The filename for this filing (e.g., "10-K_0000320193-23-000077.htm").
    /// Used when persisting to disk.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The type of SEC filing (e.g., "10-K", "10-Q", "8-K").
    /// </summary>
    public required string FilingType { get; init; }

    /// <summary>
    /// The SEC accession number that uniquely identifies this filing (e.g., "0000320193-23-000077").
    /// </summary>
    public required string AccessionNumber { get; init; }

    /// <summary>
    /// The date the filing was submitted to the SEC.
    /// </summary>
    public required DateOnly FilingDate { get; init; }

    /// <summary>
    /// The company ticker symbol or CIK associated with this filing.
    /// </summary>
    public required string CompanyIdentifier { get; init; }
}

