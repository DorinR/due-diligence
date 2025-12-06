namespace rag_experiment.Services.BackgroundJobs.Models;

/// <summary>
/// Tracks the state of a batch filing ingestion pipeline for a conversation.
/// Persisted as status.json in the conversation's ingestion folder.
/// </summary>
public class BatchProcessingState
{
    public required string ConversationId { get; set; }
    public required string UserId { get; set; }
    public required string CompanyIdentifier { get; set; }
    public required List<string> FilingTypes { get; set; }
    public BatchProcessingStatus Status { get; set; } = BatchProcessingStatus.Pending;
    public string? JobId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// List of documents being processed in this batch.
    /// </summary>
    public List<BatchDocumentInfo> Documents { get; set; } = new();
}

/// <summary>
/// Information about a single document in the batch.
/// </summary>
public class BatchDocumentInfo
{
    public required string FileName { get; set; }
    public required string FilingType { get; set; }
    public required string AccessionNumber { get; set; }
    public required DateOnly FilingDate { get; set; }
}

/// <summary>
/// Represents a text chunk with metadata for traceability back to source document.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// The source document filename.
    /// </summary>
    public required string SourceDocument { get; set; }
    
    /// <summary>
    /// Index of this chunk within the source document.
    /// </summary>
    public required int ChunkIndex { get; set; }
    
    /// <summary>
    /// The chunk text content.
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// Character offset where this chunk starts in the original document.
    /// Used for scrolling to the right position when viewing source.
    /// </summary>
    public int StartOffset { get; set; }
    
    /// <summary>
    /// Character offset where this chunk ends in the original document.
    /// </summary>
    public int EndOffset { get; set; }
}

/// <summary>
/// Represents a chunk with its generated embedding.
/// </summary>
public class ChunkEmbedding
{
    public required string SourceDocument { get; set; }
    public required int ChunkIndex { get; set; }
    public required string Text { get; set; }
    public required float[] Embedding { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
}

public enum BatchProcessingStatus
{
    Pending,
    Downloading,
    Extracting,
    Chunking,
    GeneratingEmbeddings,
    PersistingEmbeddings,
    Completed,
    Failed
}

