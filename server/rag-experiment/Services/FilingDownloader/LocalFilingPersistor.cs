using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Persists filing documents to the local filesystem.
/// Uses atomic writes (write to .tmp, then rename) to prevent corruption on failure.
/// Implements idempotent behavior - existing files are not overwritten.
/// </summary>
public class LocalFilingPersistor : IFilingPersistor
{
    /// <summary>
    /// Base directory for storing ingestion job artifacts.
    /// Files are stored at: {BaseDirectory}/{conversationId}/raw/{filename}
    /// </summary>
    private const string BaseDirectory = "/data/ingestion-jobs";

    /// <inheritdoc />
    public async Task PersistFilingsAsync(
        List<FilingDocument> documents,
        string conversationId,
        CancellationToken ct = default)
    {
        if (documents.Count == 0)
        {
            return;
        }

        var rawDirectory = GetRawDirectory(conversationId);
        EnsureDirectoryExists(rawDirectory);

        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();

            var targetPath = Path.Combine(rawDirectory, document.FileName);

            // Idempotency check: skip if file already exists
            if (File.Exists(targetPath))
            {
                continue;
            }

            await WriteFileAtomicallyAsync(targetPath, document.Content, ct);
        }
    }

    /// <summary>
    /// Writes a file atomically by writing to a temp file first, then renaming.
    /// This ensures that interrupted writes don't leave corrupt files.
    /// </summary>
    private static async Task WriteFileAtomicallyAsync(
        string targetPath,
        byte[] content,
        CancellationToken ct)
    {
        var tempPath = targetPath + ".tmp";

        try
        {
            // Write to temporary file
            await File.WriteAllBytesAsync(tempPath, content, ct);

            // Atomic rename (guaranteed atomic on Linux/macOS for same filesystem)
            File.Move(tempPath, targetPath);
        }
        catch
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* Best effort cleanup */ }
            }
            throw;
        }
    }

    /// <summary>
    /// Gets the raw documents directory path for a conversation.
    /// </summary>
    private static string GetRawDirectory(string conversationId)
    {
        return Path.Combine(BaseDirectory, conversationId, "raw");
    }

    /// <summary>
    /// Ensures the directory exists, creating it if necessary.
    /// </summary>
    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
