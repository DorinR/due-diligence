using rag_experiment.Services.Ingestion.TextExtraction;

namespace rag_experiment.Services;

/// <summary>
/// Extracts text from plain text files.
/// </summary>
public class PlainTextDocumentTextExtractor : ITextExtractor
{
    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file was not found.", filePath);

        var extension = Path.GetExtension(filePath);
        if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The specified file is not a plain text document.", nameof(filePath));

        return await File.ReadAllTextAsync(filePath);
    }
}
