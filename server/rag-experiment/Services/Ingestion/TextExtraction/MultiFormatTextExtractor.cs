using rag_experiment.Services.Ingestion.TextExtraction;

namespace rag_experiment.Services;

/// <summary>
/// Dispatches text extraction to format-specific extractors based on file extension.
/// Throws for unsupported formats.
/// </summary>
public class MultiFormatTextExtractor : ITextExtractor
{
    private readonly PdfDocumentTextExtractor _pdfExtractor;
    private readonly PlainTextDocumentTextExtractor _textExtractor;
    private readonly HtmlDocumentTextExtractor _htmlExtractor;

    public MultiFormatTextExtractor(
        PdfDocumentTextExtractor pdfExtractor,
        PlainTextDocumentTextExtractor textExtractor,
        HtmlDocumentTextExtractor htmlExtractor)
    {
        _pdfExtractor = pdfExtractor;
        _textExtractor = textExtractor;
        _htmlExtractor = htmlExtractor;
    }

    /// <inheritdoc />
    public Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
            throw new NotSupportedException("Cannot determine file type for extraction.");

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => _pdfExtractor.ExtractTextAsync(filePath),
            ".txt" => _textExtractor.ExtractTextAsync(filePath),
            ".html" or ".htm" => _htmlExtractor.ExtractTextAsync(filePath),
            _ => throw new NotSupportedException($"Unsupported document type '{extension}'.")
        };
    }
}
