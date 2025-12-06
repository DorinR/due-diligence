using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hangfire;
using rag_experiment.Domain;
using rag_experiment.Services.BackgroundJobs.Models;
using rag_experiment.Services.FilingDownloader;
using rag_experiment.Services.Ingestion.TextExtraction;
using rag_experiment.Services.Ingestion.VectorStorage;

namespace rag_experiment.Services.BackgroundJobs;

/// <summary>
/// Orchestrates the batch filing ingestion pipeline.
/// Jobs run sequentially, processing all documents at each stage before moving to the next.
/// </summary>
public class DocumentProcessingJobService : IDocumentProcessingJobService
{
    private readonly IFilingDownloader _filingDownloader;
    private readonly IFilingPersistor _filingPersistor;
    private readonly ITextExtractor _textExtractor;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingGenerationService _embeddingService;
    private readonly IEmbeddingRepository _embeddingRepository;

    private const string BaseDirectory = "/data/ingestion-jobs";

    public DocumentProcessingJobService(
        IFilingDownloader filingDownloader,
        IFilingPersistor filingPersistor,
        ITextExtractor textExtractor,
        ITextChunker textChunker,
        IEmbeddingGenerationService embeddingService,
        IEmbeddingRepository embeddingRepository)
    {
        _filingDownloader = filingDownloader;
        _filingPersistor = filingPersistor;
        _textExtractor = textExtractor;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
    }

    /// <summary>
    /// Sets up the filing ingestion pipeline by enqueueing the chain of background jobs.
    /// </summary>
    /// <param name="companyIdentifier">The company ticker symbol or CIK number.</param>
    /// <param name="filingTypes">List of filing types to download (e.g., ["10-K", "10-Q", "8-K"]).</param>
    /// <param name="userId">The user who initiated the ingestion.</param>
    /// <param name="conversationId">The conversation to associate the documents with.</param>
    /// <returns>The job ID of the final job in the chain, for tracking purposes.</returns>
    public async Task<string> SetupFilingIngestionPipeline(
        string companyIdentifier,
        List<string> filingTypes,
        int userId,
        int conversationId)
    {
        // Initialize batch state
        var state = new BatchProcessingState
        {
            ConversationId = conversationId.ToString(),
            UserId = userId.ToString(),
            CompanyIdentifier = companyIdentifier,
            FilingTypes = filingTypes,
            Status = BatchProcessingStatus.Pending
        };
        await SaveBatchStateAsync(conversationId.ToString(), state);

        // Set up the entire job chain
        var job0 = BackgroundJob.Enqueue<DocumentProcessingJobService>(
            x => x.DownloadFilings(companyIdentifier, filingTypes, conversationId));

        var job1 = BackgroundJob.ContinueJobWith<DocumentProcessingJobService>(
            job0, x => x.ExtractTextBatch(conversationId));

        var job2 = BackgroundJob.ContinueJobWith<DocumentProcessingJobService>(
            job1, x => x.ProcessChunksBatch(conversationId));

        var job3 = BackgroundJob.ContinueJobWith<DocumentProcessingJobService>(
            job2, x => x.GenerateEmbeddingsBatch(conversationId));

        var job4 = BackgroundJob.ContinueJobWith<DocumentProcessingJobService>(
            job3, x => x.PersistEmbeddingsBatch(conversationId, userId));

        // Store the final job ID for tracking
        state.JobId = job4;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        return job4;
    }

    #region Job 0: Download Filings

    /// <summary>
    /// Downloads SEC filings for a company and persists them to local storage.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task DownloadFilings(string companyIdentifier, List<string> filingTypes, int conversationId)
    {
        var state = await GetBatchStateAsync(conversationId.ToString());
        state.Status = BatchProcessingStatus.Downloading;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        try
        {
            var documents = await _filingDownloader.DownloadFilingsAsync(companyIdentifier, filingTypes);

            if (documents.Count == 0)
            {
                throw new InvalidOperationException($"No filings found for company {companyIdentifier}");
            }

            await _filingPersistor.PersistFilingsAsync(documents, conversationId.ToString());

            // Update state with document info
            state.Documents = documents.Select(d => new BatchDocumentInfo
            {
                FileName = d.FileName,
                FilingType = d.FilingType,
                AccessionNumber = d.AccessionNumber,
                FilingDate = d.FilingDate
            }).ToList();
            await SaveBatchStateAsync(conversationId.ToString(), state);
        }
        catch (Exception ex)
        {
            state.Status = BatchProcessingStatus.Failed;
            state.ErrorMessage = ex.Message;
            await SaveBatchStateAsync(conversationId.ToString(), state);
            throw;
        }
    }

    #endregion

    #region Job 1: Extract Text

    /// <summary>
    /// Extracts text from all downloaded documents in the batch.
    /// Reads from /raw/, writes to /extracted/.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExtractTextBatch(int conversationId)
    {
        var state = await GetBatchStateAsync(conversationId.ToString());
        state.Status = BatchProcessingStatus.Extracting;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        try
        {
            var rawDir = GetDirectory(conversationId.ToString(), "raw");
            var extractedDir = GetDirectory(conversationId.ToString(), "extracted");
            EnsureDirectoryExists(extractedDir);

            var files = Directory.GetFiles(rawDir);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var outputPath = Path.Combine(extractedDir, Path.ChangeExtension(fileName, ".txt"));

                // Skip if already extracted (idempotency)
                if (File.Exists(outputPath))
                {
                    continue;
                }

                var extractedText = await _textExtractor.ExtractTextAsync(filePath);
                await WriteFileAtomicallyAsync(outputPath, extractedText);
            }
        }
        catch (Exception ex)
        {
            state.Status = BatchProcessingStatus.Failed;
            state.ErrorMessage = ex.Message;
            await SaveBatchStateAsync(conversationId.ToString(), state);
            throw;
        }
    }

    #endregion

    #region Job 2: Process Chunks

    /// <summary>
    /// Chunks all extracted text documents in the batch.
    /// Reads from /extracted/, writes to /chunks/chunks.json.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessChunksBatch(int conversationId)
    {
        var state = await GetBatchStateAsync(conversationId.ToString());
        state.Status = BatchProcessingStatus.Chunking;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        try
        {
            var extractedDir = GetDirectory(conversationId.ToString(), "extracted");
            var chunksDir = GetDirectory(conversationId.ToString(), "chunks");
            EnsureDirectoryExists(chunksDir);

            var chunksPath = Path.Combine(chunksDir, "chunks.json");

            // Skip if already chunked (idempotency)
            if (File.Exists(chunksPath))
            {
                return;
            }

            var allChunks = new List<DocumentChunk>();
            var files = Directory.GetFiles(extractedDir, "*.txt");

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var sourceDocName = Path.GetFileNameWithoutExtension(fileName);
                var text = await File.ReadAllTextAsync(filePath);

                var chunks = _textChunker.ChunkText(text);
                var currentOffset = 0;

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkText = chunks[i];
                    var startOffset = text.IndexOf(chunkText, currentOffset, StringComparison.Ordinal);
                    if (startOffset < 0) startOffset = currentOffset;

                    allChunks.Add(new DocumentChunk
                    {
                        SourceDocument = sourceDocName,
                        ChunkIndex = i,
                        Text = chunkText,
                        StartOffset = startOffset,
                        EndOffset = startOffset + chunkText.Length
                    });

                    currentOffset = startOffset + chunkText.Length;
                }
            }

            var json = JsonSerializer.Serialize(allChunks, new JsonSerializerOptions { WriteIndented = true });
            await WriteFileAtomicallyAsync(chunksPath, json);
        }
        catch (Exception ex)
        {
            state.Status = BatchProcessingStatus.Failed;
            state.ErrorMessage = ex.Message;
            await SaveBatchStateAsync(conversationId.ToString(), state);
            throw;
        }
    }

    #endregion

    #region Job 3: Generate Embeddings

    /// <summary>
    /// Generates embeddings for all chunks in the batch.
    /// Reads from /chunks/chunks.json, writes to /embeddings/embeddings.json.
    /// </summary>
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10, 30, 60, 120 })]
    public async Task GenerateEmbeddingsBatch(int conversationId)
    {
        var state = await GetBatchStateAsync(conversationId.ToString());
        state.Status = BatchProcessingStatus.GeneratingEmbeddings;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        try
        {
            var chunksDir = GetDirectory(conversationId.ToString(), "chunks");
            var embeddingsDir = GetDirectory(conversationId.ToString(), "embeddings");
            EnsureDirectoryExists(embeddingsDir);

            var chunksPath = Path.Combine(chunksDir, "chunks.json");
            var embeddingsPath = Path.Combine(embeddingsDir, "embeddings.json");

            // Skip if already generated (idempotency)
            if (File.Exists(embeddingsPath))
            {
                return;
            }

            var chunksJson = await File.ReadAllTextAsync(chunksPath);
            var chunks = JsonSerializer.Deserialize<List<DocumentChunk>>(chunksJson)
                ?? throw new InvalidOperationException("Failed to deserialize chunks");

            // Generate embeddings for all chunk texts
            var chunkTexts = chunks.Select(c => c.Text).ToList();
            var embeddingsDict = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts);

            // Build chunk embeddings with metadata
            var chunkEmbeddings = chunks.Select(chunk => new ChunkEmbedding
            {
                SourceDocument = chunk.SourceDocument,
                ChunkIndex = chunk.ChunkIndex,
                Text = chunk.Text,
                Embedding = embeddingsDict[chunk.Text],
                StartOffset = chunk.StartOffset,
                EndOffset = chunk.EndOffset
            }).ToList();

            var json = JsonSerializer.Serialize(chunkEmbeddings, new JsonSerializerOptions { WriteIndented = true });
            await WriteFileAtomicallyAsync(embeddingsPath, json);
        }
        catch (Exception ex)
        {
            state.Status = BatchProcessingStatus.Failed;
            state.ErrorMessage = ex.Message;
            await SaveBatchStateAsync(conversationId.ToString(), state);
            throw;
        }
    }

    #endregion

    #region Job 4: Persist Embeddings

    /// <summary>
    /// Persists all generated embeddings to the database.
    /// Reads from /embeddings/embeddings.json, writes to database.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)]
    public async Task PersistEmbeddingsBatch(int conversationId, int userId)
    {
        var state = await GetBatchStateAsync(conversationId.ToString());
        state.Status = BatchProcessingStatus.PersistingEmbeddings;
        await SaveBatchStateAsync(conversationId.ToString(), state);

        try
        {
            var embeddingsDir = GetDirectory(conversationId.ToString(), "embeddings");
            var embeddingsPath = Path.Combine(embeddingsDir, "embeddings.json");

            var embeddingsJson = await File.ReadAllTextAsync(embeddingsPath);
            var chunkEmbeddings = JsonSerializer.Deserialize<List<ChunkEmbedding>>(embeddingsJson)
                ?? throw new InvalidOperationException("Failed to deserialize embeddings");

            // Build upsert items
            var items = chunkEmbeddings.Select(ce => new EmbeddingUpsertItem
            {
                Text = ce.Text,
                Vector = ce.Embedding,
                DocumentId = ce.SourceDocument,
                UserId = userId,
                ConversationId = conversationId,
                DocumentTitle = ce.SourceDocument,
                ChunkIndex = ce.ChunkIndex,
                ChunkHash = ComputeSha256(ce.Text),
                Owner = EmbeddingOwner.UserDocument
            }).ToList();

            await _embeddingRepository.UpsertEmbeddingsAsync(items);

            // Mark as completed
            state.Status = BatchProcessingStatus.Completed;
            state.CompletedAt = DateTime.UtcNow;
            await SaveBatchStateAsync(conversationId.ToString(), state);
        }
        catch (Exception ex)
        {
            state.Status = BatchProcessingStatus.Failed;
            state.ErrorMessage = ex.Message;
            await SaveBatchStateAsync(conversationId.ToString(), state);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private static string GetDirectory(string conversationId, string subFolder)
    {
        return Path.Combine(BaseDirectory, conversationId, subFolder);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static async Task WriteFileAtomicallyAsync(string targetPath, string content)
    {
        var tempPath = targetPath + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, content);
            File.Move(tempPath, targetPath);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* Best effort */ }
            }
            throw;
        }
    }

    private static byte[] ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input.Replace("\r\n", "\n").Replace("\r", "\n"));
        return SHA256.HashData(bytes);
    }

    private async Task<BatchProcessingState> GetBatchStateAsync(string conversationId)
    {
        var stateDir = GetDirectory(conversationId, "");
        var statePath = Path.Combine(stateDir, "status.json");

        if (File.Exists(statePath))
        {
            var json = await File.ReadAllTextAsync(statePath);
            return JsonSerializer.Deserialize<BatchProcessingState>(json)
                ?? throw new InvalidOperationException("Failed to deserialize batch state");
        }

        throw new InvalidOperationException($"Batch state not found for conversation {conversationId}");
    }

    private async Task SaveBatchStateAsync(string conversationId, BatchProcessingState state)
    {
        var stateDir = GetDirectory(conversationId, "");
        EnsureDirectoryExists(stateDir);

        var statePath = Path.Combine(stateDir, "status.json");
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        await WriteFileAtomicallyAsync(statePath, json);
    }

    #endregion
}
