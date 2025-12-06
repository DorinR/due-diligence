using rag_experiment.Services.FilingDownloader.Models;

namespace rag_experiment.Services.FilingDownloader;

/// <summary>
/// Interface for persisting downloaded filing documents to storage.
/// Implementations handle the specifics of the storage backend (e.g., local filesystem, S3).
/// </summary>
public interface IFilingPersistor
{
    /// <summary>
    /// Persists a collection of filing documents to storage.
    /// Implementations should be idempotent - persisting the same document multiple times
    /// should not create duplicates (upsert behavior).
    /// </summary>
    /// <param name="documents">The filing documents to persist.</param>
    /// <param name="conversationId">The conversation ID used to organize storage (e.g., folder path).</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    Task PersistFilingsAsync(
        List<FilingDocument> documents,
        string conversationId,
        CancellationToken ct = default);
}

