using System.Net;
using System.Text.RegularExpressions;
using rag_experiment.Services.Ingestion.TextExtraction;

namespace rag_experiment.Services;

/// <summary>
/// Extracts readable text content from HTML/HTM documents by stripping tags and scripts/styles.
/// </summary>
public class HtmlDocumentTextExtractor : ITextExtractor
{
    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified file was not found.", filePath);

        var extension = Path.GetExtension(filePath);
        if (!string.Equals(extension, ".html", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".htm", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The specified file is not an HTML document.", nameof(filePath));
        }

        var html = await File.ReadAllTextAsync(filePath);

        // Remove script and style blocks
        var withoutScripts = Regex.Replace(html, "<script[\\s\\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        var withoutStyles = Regex.Replace(withoutScripts, "<style[\\s\\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

        // Strip all tags
        var withoutTags = Regex.Replace(withoutStyles, "<[^>]+>", " ");

        // Decode HTML entities and normalize whitespace
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var normalized = Regex.Replace(decoded, "\\s+", " ").Trim();

        return normalized;
    }
}
