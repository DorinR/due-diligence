using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using rag_experiment.Services;
using rag_experiment.Domain;
using rag_experiment.Services.Query.Models;
using rag_experiment.Services.LlmClient.Models;
using rag_experiment.Services.Ingestion.VectorStorage;
using rag_experiment.Repositories.Documents;
using System.Security.Cryptography;
using System.Text;

namespace rag_experiment.Controllers;

/// <summary>
/// Controller responsible for training operations including document processing and embedding generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly EmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingGenerationService _openAiEmbeddingGenerationService;
    private readonly ITextProcessor _textProcessor;
    private readonly ITextChunker _textChunker;
    private readonly AppDbContext _dbContext;
    private readonly RagSettings _ragSettings;
    private readonly ILlmClientFactory _llmClientFactory;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<TrainingController> _logger;

    /// <summary>
    /// Initializes a new instance of the TrainingController
    /// </summary>
    /// <param name="embeddingRepository">Repository for managing embeddings</param>
    /// <param name="openAiEmbeddingGenerationService">Service for generating embeddings</param>
    /// <param name="textProcessor">Service for text processing operations</param>
    /// <param name="textChunker">Service for text chunking operations</param>
    /// <param name="dbContext">Database context for data operations</param>
    /// <param name="ragSettings">RAG configuration settings</param>
    /// <param name="llmClientFactory">Factory for creating LLM clients</param>
    /// <param name="documentRepository">Repository for document operations</param>
    /// <param name="logger">Logger for controller operations</param>
    public TrainingController(
        EmbeddingRepository embeddingRepository,
        IEmbeddingGenerationService openAiEmbeddingGenerationService,
        ITextProcessor textProcessor,
        ITextChunker textChunker,
        AppDbContext dbContext,
        IOptions<RagSettings> ragSettings,
        ILlmClientFactory llmClientFactory,
        IDocumentRepository documentRepository,
        ILogger<TrainingController> logger)
    {
        _embeddingRepository = embeddingRepository;
        _openAiEmbeddingGenerationService = openAiEmbeddingGenerationService;
        _textProcessor = textProcessor;
        _textChunker = textChunker;
        _dbContext = dbContext;
        _ragSettings = ragSettings.Value;
        _llmClientFactory = llmClientFactory;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Trains the system by processing all TXT files found in subdirectories of the specified training folder
    /// and creating SystemKnowledgeBase embeddings for them. Also creates Document records with full text content.
    /// </summary>
    /// <param name="request">Training request containing the folder name</param>
    /// <returns>Training results including number of documents processed</returns>
    [HttpPost("train")]
    public async Task<IActionResult> Train([FromBody] TrainRequest request)
    {
        if (string.IsNullOrEmpty(request.FolderName))
            return BadRequest("FolderName is required");

        try
        {
            // Get the training folder path
            var trainingFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Training", request.FolderName);

            if (!Directory.Exists(trainingFolderPath))
                return NotFound($"Training folder '{request.FolderName}' not found");

            var allTextFilePaths = Directory.GetFiles(trainingFolderPath, "*.txt", SearchOption.AllDirectories);

            if (allTextFilePaths.Length == 0)
                return Ok(new
                {
                    message = "No TXT files found in the training folder",
                    folderName = request.FolderName,
                    documentsProcessed = 0
                });

            var documentsProcessed = 0;
            var totalChunks = 0;
            var processedFiles = new List<string>();

            // Use null for system training data (no user or conversation association)
            foreach (var filePath in allTextFilePaths)
            {
                var fileName = Path.GetFileName(filePath);

                try
                {
                    // Read text from TXT file
                    var rawText = await System.IO.File.ReadAllTextAsync(filePath);

                    if (string.IsNullOrWhiteSpace(rawText))
                    {
                        Console.WriteLine($"Warning: No text found in {fileName}");
                        continue;
                    }

                    // Sanitize text to remove null bytes and problematic characters
                    var text = SanitizeTextForDatabase(rawText);

                    // Create Document record in database
                    var document = new Document
                    {
                        FileName = $"training_{request.FolderName}_{fileName}",
                        OriginalFileName = fileName,
                        ContentType = "text/plain",
                        FileSize = new FileInfo(filePath).Length,
                        FilePath = filePath,
                        Description = $"Training document from {request.FolderName} folder",
                        DocumentText = text, // Store the full sanitized text
                        TrainingFolderName = request.FolderName,
                        ConversationId = null,
                        UploadedAt = DateTime.UtcNow
                    };

                    _dbContext.Documents.Add(document);
                    await _dbContext.SaveChangesAsync(); // Save to get the document ID

                    // Process the text using the same pipeline as document ingestion
                    var processedText = _textProcessor.ProcessText(text);

                    // Split into chunks using configured settings and proper semantic chunking
                    var chunks = _textChunker.ChunkText(processedText);

                    if (chunks.Count > 0)
                    {
                        // Generate embeddings for all chunks in batch
                        var embeddingResults = await _openAiEmbeddingGenerationService.GenerateEmbeddingsAsync(chunks);

                        // Prepare batch upsert items
                        var upsertItems = new List<EmbeddingUpsertItem>();
                        for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                        {
                            var chunkText = chunks[chunkIndex];
                            var embedding = embeddingResults[chunkText];
                            var chunkHash = GenerateChunkHash(chunkText);

                            upsertItems.Add(new EmbeddingUpsertItem
                            {
                                Text = chunkText,
                                Vector = embedding,
                                Owner = EmbeddingOwner.SystemKnowledgeBase,
                                UserId = null, // No user association for system training data
                                ConversationId = null, // No conversation association for system training data
                                DocumentId = document.Id.ToString(),
                                ChunkIndex = chunkIndex,
                                ChunkHash = chunkHash,
                                DocumentTitle = fileName,
                                TrainingFolderName = request.FolderName
                            });
                        }

                        // Batch upsert all embeddings for this document
                        await _embeddingRepository.UpsertDocumentEmbeddingsAsync(upsertItems);
                        totalChunks += chunks.Count;
                    }

                    documentsProcessed++;
                    processedFiles.Add(fileName);

                    Console.WriteLine(
                        $"Processed training document: {fileName} ({chunks.Count} chunks) - Document ID: {document.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {fileName}: {ex.Message}");
                    // Continue with other files
                }
            }

            return Ok(new
            {
                message = "Training completed successfully",
                folderName = request.FolderName,
                documentsProcessed = documentsProcessed,
                totalChunks = totalChunks,
                processedFiles = processedFiles,
                note = "Document records created with full text content stored in DocumentText column"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred during training: {ex.Message}");
        }
    }


    /// <summary>
    /// Sanitizes text by removing null bytes and other problematic characters that can cause PostgreSQL encoding issues
    /// </summary>
    /// <param name="text">Text to sanitize</param>
    /// <returns>Sanitized text safe for PostgreSQL storage</returns>
    private string SanitizeTextForDatabase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Remove null bytes and other control characters that cause PostgreSQL encoding issues
        var sanitized = text
            .Replace("\0", "") // Remove null bytes
            .Replace("\uFFFE", "") // Remove byte order mark
            .Replace("\uFFFF", ""); // Remove invalid Unicode character

        // Remove other problematic control characters except common whitespace
        var result = new StringBuilder();
        foreach (var c in sanitized)
            // Keep printable characters, common whitespace (space, tab, newline, carriage return)
            if (char.IsControl(c))
            {
                if (c == '\t' || c == '\n' || c == '\r')
                    result.Append(c);
                // Skip other control characters
            }
            else
            {
                result.Append(c);
            }

        return result.ToString();
    }

    /// <summary>
    /// Generates a SHA256 hash of the chunk text for change detection
    /// </summary>
    /// <param name="chunkText">The text content to hash</param>
    /// <returns>SHA256 hash as byte array</returns>
    private byte[] GenerateChunkHash(string chunkText)
    {
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(chunkText));
        }
    }

    /// <summary>
    /// Generates legal-style titles for all documents in the database using the Fast LLM model.
    /// The LLM analyzes document content and creates titles in the format: "Party v Party, Court, Year".
    /// Only processes documents that have content (DocumentText is not null/empty).
    /// </summary>
    /// <returns>Summary of title generation process including success/failure counts</returns>
    [HttpPost("generate-titles")]
    public async Task<IActionResult> GenerateTitles()
    {
        try
        {
            _logger.LogInformation("Starting document title generation process");

            // Fetch all documents from database
            var allDocuments = await _documentRepository.GetAllAsync();

            if (allDocuments.Count == 0)
                return Ok(new
                {
                    message = "No documents found in database",
                    documentsProcessed = 0,
                    titlesGenerated = 0,
                    errors = 0
                });

            // Filter to documents with content
            var documentsWithContent = allDocuments
                .Where(d => !string.IsNullOrWhiteSpace(d.DocumentText))
                .ToList();

            _logger.LogInformation("Found {TotalDocs} total documents, {WithContent} have content",
                allDocuments.Count, documentsWithContent.Count);

            // Create Fast tier LLM client (cost-effective for this task)
            var llmClient = _llmClientFactory.CreateClient(LlmModelTier.Fast);

            var titlesGenerated = 0;
            var errors = 0;
            var results = new List<object>();

            foreach (var document in documentsWithContent)
                try
                {
                    _logger.LogDebug("Generating title for document ID: {DocId}, File: {FileName}",
                        document.Id, document.OriginalFileName);

                    // Take first 2000 characters of document for title generation (cost optimization)
                    var contentPreview = document.DocumentText!.Length > 2000
                        ? document.DocumentText.Substring(0, 2000) + "..."
                        : document.DocumentText;

                    // Craft prompt for legal title generation
                    var prompt =
                        @"Based on the legal document content provided, generate a professional legal citation-style title.
                        The title should follow this format: ""Party A v. Party B, Court Name, Year"" (e.g., ""Smith v. Jones, Court of Appeals, 2023"").

                        CRITICAL FORMATTING RULES - FOLLOW EXACTLY:

                        1. PROPER CASE IS MANDATORY:
                        - Write all party names in proper title case (e.g., 'Smith', 'Commission Scolaire', 'Procureure Générale')
                        - WRONG: 'RMIMN F-11' or 'PROCUREURE GÉNÉRALE' (all caps)
                        - RIGHT: 'Rmimn F-11' or 'Procureure Générale' (proper case)
                        - Only use all caps for acronyms like 'USA', 'FBI', 'NATO'

                        2. NO SPACES BETWEEN LETTERS:
                        - WRONG: 'C O U R T' or 'S U P É R I E U R E'
                        - RIGHT: 'Court' or 'Supérieure'

                        3. NEVER USE PLACEHOLDER TEXT:
                        - NEVER write 'Unknown', 'Party A', 'Party B', 'Court Name', 'Year', etc.
                        - If you can't find a party name, court, or year, OMIT that entire component
                        - WRONG: 'R-4 v. Unknown, Unknown, 2023'
                        - RIGHT: 'R-4, 2023' (omit the unknown party and court)
                        - If you only have a case number like 'R-4' or 'F-11', that's fine to use alone

                        4. EXTRACT REAL INFORMATION ONLY:
                        - Only include information that actually appears in the document
                        - If the document doesn't clearly identify parties, court, or year, leave those parts out
                        - A partial title is better than a title with placeholder text

                        Return ONLY the title, nothing else. No explanations, no notes, just the properly formatted title.

                        Document content:";

                    // Generate title using LLM
                    var generatedTitle = await llmClient.GenerateResponseAsync(
                        prompt,
                        contentPreview);

                    // Clean up the title (remove extra quotes, newlines, etc.)
                    generatedTitle = generatedTitle.Trim().Trim('"', '\'', '\n', '\r');

                    // Limit title length to reasonable size
                    if (generatedTitle.Length > 200)
                        generatedTitle = generatedTitle.Substring(0, 197) + "...";

                    // Update document with generated title
                    document.Title = generatedTitle;
                    titlesGenerated++;

                    _logger.LogInformation("Generated title for document {DocId}: {Title}",
                        document.Id, generatedTitle);

                    results.Add(new
                    {
                        documentId = document.Id,
                        fileName = document.OriginalFileName,
                        generatedTitle = generatedTitle,
                        success = true
                    });
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error generating title for document ID: {DocId}", document.Id);

                    results.Add(new
                    {
                        documentId = document.Id,
                        fileName = document.OriginalFileName,
                        error = ex.Message,
                        success = false
                    });
                }

            // Save all changes to database
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Title generation completed. Processed: {Total}, Success: {Success}, Errors: {Errors}",
                documentsWithContent.Count, titlesGenerated, errors);

            return Ok(new
            {
                message = "Document title generation completed",
                totalDocuments = allDocuments.Count,
                documentsWithContent = documentsWithContent.Count,
                documentsProcessed = documentsWithContent.Count,
                titlesGenerated = titlesGenerated,
                errors = errors,
                modelUsed = "Fast (GPT-5 Nano)",
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during title generation process");
            return StatusCode(500, $"An error occurred during title generation: {ex.Message}");
        }
    }
}

/// <summary>
/// Request model for training operations
/// </summary>
public class TrainRequest
{
    /// <summary>
    /// The folder name containing training documents
    /// </summary>
    public required string FolderName { get; set; }
}