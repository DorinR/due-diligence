using rag_experiment.Services.BackgroundJobs.Models;

namespace rag_experiment.Hubs.Models;

/// <summary>
/// Real-time progress update sent to clients during document processing.
/// Represents a single stage update in the processing pipeline.
/// </summary>
public record DocumentProcessingUpdate
{
    /// <summary>
    /// Current processing stage (e.g., "Downloading", "Extracting", "Chunking")
    /// </summary>
    public required BatchProcessingStatus Stage { get; init; }

    /// <summary>
    /// Human-readable message describing the current operation
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public required int ProgressPercent { get; init; }

    /// <summary>
    /// Number of documents successfully processed so far
    /// </summary>
    public int? DocumentsProcessed { get; init; }

    /// <summary>
    /// Total number of documents being processed
    /// </summary>
    public int? TotalDocuments { get; init; }

    /// <summary>
    /// Timestamp when this update was generated
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Final result notification sent when processing completes successfully.
/// </summary>
public record ProcessingCompleteResult
{
    /// <summary>
    /// Total number of documents processed
    /// </summary>
    public required int TotalDocuments { get; init; }

    /// <summary>
    /// Number of documents successfully processed
    /// </summary>
    public required int SuccessfulDocuments { get; init; }

    /// <summary>
    /// Number of documents that failed processing
    /// </summary>
    public required int FailedDocuments { get; init; }

    /// <summary>
    /// Total duration of the processing pipeline
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Timestamp when processing completed
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Error notification sent when processing fails.
/// </summary>
public record ProcessingErrorResult
{
    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// The stage at which the error occurred
    /// </summary>
    public required BatchProcessingStatus Stage { get; init; }

    /// <summary>
    /// Number of documents successfully processed before failure
    /// </summary>
    public int? DocumentsProcessed { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
