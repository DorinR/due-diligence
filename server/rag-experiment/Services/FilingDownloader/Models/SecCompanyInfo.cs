namespace rag_experiment.Services.FilingDownloader.Models;

/// <summary>
/// Represents a company entry returned by the SEC company directory feed.
/// </summary>
public class SecCompanyInfo
{
    /// <summary>
    /// Company CIK as a zero-padded string.
    /// </summary>
    public required string Cik { get; init; }

    /// <summary>
    /// Raw numeric CIK returned by the SEC feed.
    /// </summary>
    public required long CikNumber { get; init; }

    /// <summary>
    /// Company display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Trading ticker, if present.
    /// </summary>
    public string? Ticker { get; init; }

    /// <summary>
    /// Exchange name, if present.
    /// </summary>
    public string? Exchange { get; init; }
}
