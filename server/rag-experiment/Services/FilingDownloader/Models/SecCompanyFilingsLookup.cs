namespace rag_experiment.Services.FilingDownloader.Models;

public class SecCompanyFilingsLookup
{
    public required string Cik { get; init; }

    public string? Name { get; init; }

    public IReadOnlyList<string> Tickers { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Exchanges { get; init; } = Array.Empty<string>();

    public IReadOnlyList<SecAvailableFilingType> AvailableFilingTypes { get; init; } =
        Array.Empty<SecAvailableFilingType>();
}

public class SecAvailableFilingType
{
    public required string FormType { get; init; }

    public required int FilingCount { get; init; }

    public DateOnly? LatestFilingDate { get; init; }
}
