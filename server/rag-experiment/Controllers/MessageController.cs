using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rag_experiment.Models;
using rag_experiment.Services;
using rag_experiment.Services.Auth;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using rag_experiment.Services.Ingestion.VectorStorage;
using rag_experiment.Repositories.Documents;
using rag_experiment.Repositories.Conversations;
using rag_experiment.Services.Query;
using System.Text;
using System.Linq;

namespace rag_experiment.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/conversations/{conversationId}/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IUserContext _userContext;
        private readonly EmbeddingRepository _embeddingRepository;
        private readonly IEmbeddingGenerationService _embeddingGenerationService;
        private readonly IQueryPreprocessor _queryPreprocessor;
        private readonly ILlmService _llmService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IQueryIntentClassifier _queryIntentClassifier;
        private readonly IAdaptiveRetrievalStrategy _adaptiveRetrievalStrategy;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            AppDbContext dbContext,
            IUserContext userContext,
            EmbeddingRepository embeddingRepository,
            IEmbeddingGenerationService embeddingGenerationService,
            IQueryPreprocessor queryPreprocessor,
            ILlmService llmService,
            IDocumentRepository documentRepository,
            IConversationRepository conversationRepository,
            IQueryIntentClassifier queryIntentClassifier,
            IAdaptiveRetrievalStrategy adaptiveRetrievalStrategy,
            ILogger<MessageController> logger)
        {
            _dbContext = dbContext;
            _userContext = userContext;
            _embeddingRepository = embeddingRepository;
            _embeddingGenerationService = embeddingGenerationService;
            _queryPreprocessor = queryPreprocessor;
            _llmService = llmService;
            _documentRepository = documentRepository;
            _conversationRepository = conversationRepository;
            _queryIntentClassifier = queryIntentClassifier;
            _adaptiveRetrievalStrategy = adaptiveRetrievalStrategy;
            _logger = logger;
        }

        /// <summary>
        /// Formats conversation messages into a readable context string for the LLM
        /// </summary>
        /// <param name="messages">List of conversation messages</param>
        /// <returns>Formatted conversation history string</returns>
        private string FormatConversationHistory(List<Message> messages)
        {
            if (!messages.Any())
                return string.Empty;

            var conversationBuilder = new StringBuilder();
            conversationBuilder.AppendLine("=== CONVERSATION HISTORY ===");

            foreach (var message in messages)
            {
                string roleLabel = message.Role switch
                {
                    MessageRole.User => "USER",
                    MessageRole.Assistant => "ASSISTANT",
                    MessageRole.System => "SYSTEM",
                    _ => "UNKNOWN"
                };

                conversationBuilder.AppendLine($"[{roleLabel}]: {message.Content}");
                conversationBuilder.AppendLine();
            }

            conversationBuilder.AppendLine("=== END CONVERSATION HISTORY ===");
            conversationBuilder.AppendLine();

            return conversationBuilder.ToString();
        }

        /// <summary>
        /// Calculates the cosine similarity between two embedding vectors.
        /// </summary>
        /// <param name="a">First embedding vector</param>
        /// <param name="b">Second embedding vector</param>
        /// <returns>Cosine similarity score between 0 and 1, or 0 if vectors are different lengths</returns>
        private float CalculateCosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                return 0; // Return minimum similarity score instead of throwing exception

            float dotProduct = 0;
            float normA = 0;
            float normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            // Handle zero vectors
            if (normA == 0 || normB == 0)
                return 0;

            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        /// <summary>
        /// Converts a byte array blob back to a float array.
        /// </summary>
        /// <param name="blob">The byte array blob</param>
        /// <returns>The float array</returns>
        private float[] ConvertFromBlob(byte[] blob)
        {
            // Convert the byte array back to a float array
            float[] embeddingData = new float[blob.Length / sizeof(float)];

            // Copy the byte array to the float array
            Buffer.BlockCopy(blob, 0, embeddingData, 0, blob.Length);

            return embeddingData;
        }

        /// <summary>
        /// Adds a message to a conversation. For User messages, automatically queries the knowledge base
        /// and saves the assistant response with sources. For other roles, only saves the message.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <param name="request">The message request containing role, content, and optional metadata</param>
        /// <returns>200 OK with message details, or error response if operation fails</returns>
        [HttpPost]
        public async Task<IActionResult> AddMessage(int conversationId, [FromBody] AddMessageRequest request)
        {
            try
            {
                // Debug: Log the raw request body
                Request.EnableBuffering();
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                _logger.LogInformation("Raw request body: {RawBody}", rawBody);
                _logger.LogInformation("Parsed request - Role: {Role}, Content: {Content}, Metadata: {Metadata}",
                    request?.Role, request?.Content, request?.Metadata);

                // Debug: Check ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid:");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            _logger.LogWarning("Field: {Field}, Error: {Error}", state.Key, error.ErrorMessage);
                        }
                    }

                    return BadRequest(ModelState);
                }

                var userId = _userContext.GetCurrentUserId();

                // Verify conversation exists and belongs to user
                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

                if (conversation == null)
                    return NotFound("Conversation not found");

                // Ensure content is not null (already validated by ModelState, but compiler doesn't know)
                if (request == null || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Content cannot be empty");
                }

                var userContent = request.Content;

                // Save the user message
                var message = new Message
                {
                    ConversationId = conversationId,
                    Role = request.Role,
                    Content = userContent,
                    Metadata = request.Metadata,
                    Timestamp = DateTime.UtcNow
                };

                _dbContext.Messages.Add(message);

                // Update conversation's UpdatedAt timestamp
                conversation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Saved {Role} message with ID {MessageId} to conversation {ConversationId}",
                    request.Role, message.Id, conversationId);

                // If this is a User message, query the knowledge base and save assistant response
                if (request.Role == MessageRole.User)
                {
                    _logger.LogInformation("Processing User message - querying knowledge base for conversation {ConversationId}",
                        conversationId);

                    try
                    {
                        // Get conversation history (including the message we just saved)
                        var conversationMessages = await _conversationRepository.GetMessagesAsync(conversationId);
                        var conversationHistory = FormatConversationHistory(conversationMessages);

                        // 1. Classify query intent for adaptive retrieval
                        var intentResult = await _queryIntentClassifier.ClassifyQueryAsync(userContent);
                        _logger.LogInformation("Query intent classified as {Intent}: {Reasoning}",
                            intentResult.Intent, intentResult.Reasoning);

                        // 2. Get retrieval configuration based on intent
                        var retrievalConfig = _adaptiveRetrievalStrategy.GetConfigForIntent(intentResult.Intent, userContent);

                        var maxK = retrievalConfig.MaxK;
                        var minSimilarity = retrievalConfig.MinSimilarity;

                        // 3. Pre-process the query with conversation context
                        string processedQuery = string.IsNullOrEmpty(conversationHistory)
                            ? await _queryPreprocessor.ProcessQueryAsync(userContent)
                            : await _queryPreprocessor.ProcessQueryAsync(userContent, conversationHistory);

                        // 4. Generate embedding for the processed query
                        var queryEmbedding = await _embeddingGenerationService.GenerateEmbeddingAsync(processedQuery);

                        // 5. Get embeddings for referenced documents (if provided)
                        var referencedEmbeddings = new List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>();
                        if (request.ReferencedDocumentIds != null && request.ReferencedDocumentIds.Any())
                        {
                            var referencedDocIds = request.ReferencedDocumentIds.Select(id => id.ToString()).ToList();

                            // Get all embeddings for the referenced documents from SystemKnowledgeBase
                            var referencedEmbeddingEntities = await _dbContext.Embeddings
                                .Where(e => referencedDocIds.Contains(e.DocumentId) &&
                                           e.Owner == EmbeddingOwner.SystemKnowledgeBase)
                                .ToListAsync();

                            // Calculate similarity for each referenced embedding
                            foreach (var embedding in referencedEmbeddingEntities)
                            {
                                var embeddingVector = ConvertFromBlob(embedding.EmbeddingData);
                                var similarity = CalculateCosineSimilarity(queryEmbedding, embeddingVector);

                                referencedEmbeddings.Add((
                                    embedding.Text,
                                    embedding.DocumentId,
                                    embedding.DocumentTitle,
                                    similarity
                                ));
                            }

                            _logger.LogInformation("Retrieved {Count} embeddings from {DocCount} referenced documents",
                                referencedEmbeddings.Count, request.ReferencedDocumentIds.Count);
                        }

                        // 6. Adaptive retrieval with threshold
                        var topKSimilarEmbeddings = await _embeddingRepository.FindSimilarEmbeddingsAdaptiveAsync(
                            queryEmbedding,
                            maxK,
                            minSimilarity);

                        _logger.LogInformation("Retrieved {Count} embeddings with threshold {Threshold}",
                            topKSimilarEmbeddings.Count, minSimilarity);

                        // 7. Merge referenced embeddings with adaptive retrieval results
                        // Create a dictionary to track chunks by document+text to avoid duplicates
                        var mergedEmbeddingsDict = new Dictionary<string, (string Text, string DocumentId, string DocumentTitle, float Similarity)>();

                        // Add adaptive retrieval results first
                        foreach (var embedding in topKSimilarEmbeddings)
                        {
                            var key = $"{embedding.DocumentId}|{embedding.Text}";
                            if (!mergedEmbeddingsDict.ContainsKey(key) || mergedEmbeddingsDict[key].Similarity < embedding.Similarity)
                            {
                                mergedEmbeddingsDict[key] = embedding;
                            }
                        }

                        // Add referenced embeddings (overwrite if duplicate)
                        foreach (var embedding in referencedEmbeddings)
                        {
                            var key = $"{embedding.DocumentId}|{embedding.Text}";
                            // Always include referenced embeddings, overwriting adaptive results if duplicate
                            mergedEmbeddingsDict[key] = embedding;
                        }

                        // Convert back to list and order by similarity
                        var mergedEmbeddings = mergedEmbeddingsDict.Values
                            .OrderByDescending(e => e.Similarity)
                            .ToList();

                        _logger.LogInformation("Merged embeddings: {TotalCount} total (Adaptive: {AdaptiveCount}, Referenced: {ReferencedCount})",
                            mergedEmbeddings.Count, topKSimilarEmbeddings.Count, referencedEmbeddings.Count);

                        // 8. Aggregate by document for source tracking (using merged embeddings)
                        var documentContributions = mergedEmbeddings
                            .GroupBy(doc => doc.DocumentId)
                            .Select(g => new
                            {
                                DocumentId = int.Parse(g.Key),
                                ChunksUsed = g.Count(),
                                AvgSimilarity = g.Average(d => d.Similarity),
                                MaxSimilarity = g.Max(d => d.Similarity),
                                DocumentTitle = g.First().DocumentTitle,
                                Chunks = g.ToList()
                            })
                            .OrderByDescending(d => d.MaxSimilarity)
                            .ToList();

                        // Ensure all referenced documents are included as sources, even if they weren't in merged results
                        if (request.ReferencedDocumentIds != null && request.ReferencedDocumentIds.Any())
                        {
                            var referencedDocIdsSet = request.ReferencedDocumentIds.ToHashSet();
                            var existingDocIds = documentContributions.Select(d => d.DocumentId).ToHashSet();

                            foreach (var referencedDocId in referencedDocIdsSet)
                            {
                                if (!existingDocIds.Contains(referencedDocId))
                                {
                                    // Get document info for referenced document that wasn't in results
                                    var referencedDoc = await _documentRepository.GetByIdAsync(referencedDocId);
                                    if (referencedDoc != null)
                                    {
                                        documentContributions.Add(new
                                        {
                                            DocumentId = referencedDocId,
                                            ChunksUsed = 0,
                                            AvgSimilarity = 0f,
                                            MaxSimilarity = 0f,
                                            DocumentTitle = referencedDoc.Title ?? referencedDoc.OriginalFileName,
                                            Chunks = new List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>()
                                        });
                                    }
                                }
                            }
                        }

                        // Get the IDs of all of the documents from the top-K embeddings
                        var relatedDocumentsIds = documentContributions.Select(d => d.DocumentId).ToList();

                        // Get the documents
                        var relatedDocuments = await _documentRepository.GetByIdsAsync(relatedDocumentsIds);

                        // 9. Format the retrieved passages (using merged embeddings)
                        var retrievedResults = mergedEmbeddings.Select(doc => new
                        {
                            fullDocumentText = doc.Text,
                            documentId = doc.DocumentId,
                            documentTitle = doc.DocumentTitle,
                            similarity = doc.Similarity
                        }).ToList();

                        string llmResponse;

                        // 10. Handle exhaustive mode differently - skip LLM context building
                        if (intentResult.Intent == QueryIntent.Exhaustive)
                        {
                            // Count unique documents found
                            var documentCount = documentContributions.Count;

                            // Generate LLM response without document chunks in context
                            // The LLM will generate a natural response about finding X documents
                            var exhaustivePrompt = $"The user asked: \"{userContent}\". Generate a brief, natural response informing them that you found {documentCount} documents related to their query. Do not mention specific document details, just acknowledge that {documentCount} documents were found and will be provided as sources.";

                            llmResponse = await _llmService.GenerateResponseAsync(exhaustivePrompt, conversationHistory ?? "");

                            _logger.LogInformation("Exhaustive mode: Generated response for {DocumentCount} documents without context", documentCount);
                        }
                        else
                        {
                            // Regular mode: Combine conversation history and document chunks into a single context string
                            var contextBuilder = new StringBuilder();

                            // Add conversation history first
                            if (!string.IsNullOrEmpty(conversationHistory))
                            {
                                contextBuilder.AppendLine(conversationHistory);
                            }

                            // Add retrieved document chunks
                            contextBuilder.AppendLine("=== KNOWLEDGE BASE DOCUMENTS ===");
                            foreach (var doc in retrievedResults)
                            {
                                contextBuilder.AppendLine($"--- {doc.documentTitle} ---");
                                contextBuilder.AppendLine(doc.fullDocumentText ?? "");
                                contextBuilder.AppendLine();
                            }

                            string combinedContext = contextBuilder.ToString();

                            _logger.LogInformation("Combined context: {Length} chars, Estimated tokens: {Tokens}",
                                combinedContext.Length, combinedContext.Length / 4);

                            // Generate LLM response using the combined context (conversation + knowledge base)
                            llmResponse = await _llmService.GenerateResponseAsync(userContent, combinedContext);
                        }

                        // 11. Save assistant message WITH source citations
                        var assistantMessage = new Message
                        {
                            Role = MessageRole.Assistant,
                            Content = llmResponse,
                            ConversationId = conversationId,
                            Timestamp = DateTime.UtcNow
                        };

                        // Add source citations
                        int order = 0;
                        foreach (var docContribution in documentContributions)
                        {
                            assistantMessage.Sources.Add(new MessageSource
                            {
                                DocumentId = docContribution.DocumentId,
                                RelevanceScore = docContribution.MaxSimilarity,
                                ChunksUsed = docContribution.ChunksUsed,
                                Order = order++
                            });
                        }

                        // Save the assistant message with sources
                        await _conversationRepository.AddMessageAsync(assistantMessage);

                        // Update conversation's UpdatedAt timestamp again
                        conversation.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation("Saved assistant message with {SourceCount} source citations",
                            assistantMessage.Sources.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while querying knowledge base for User message");
                        // Return error - frontend will handle removing optimistic update
                        return StatusCode(500, $"An error occurred while querying the knowledge base: {ex.Message}");
                    }
                }

                // Return success response
                return Ok(new
                {
                    message.Id,
                    message.Role,
                    message.Content,
                    message.Timestamp,
                    message.Metadata,
                    message.ConversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding message");
                return StatusCode(500, $"An error occurred while adding the message: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                // Verify conversation exists and belongs to user
                var conversationExists = await _dbContext.Conversations
                    .AnyAsync(c => c.Id == conversationId && c.UserId == userId);

                if (!conversationExists)
                    return NotFound("Conversation not found");

                var messages = await _dbContext.Messages
                    .Include(m => m.Sources)
                    .ThenInclude(s => s.Document)
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        m.Id,
                        m.Role,
                        m.Content,
                        m.Timestamp,
                        m.Metadata,
                        Sources = m.Sources.OrderBy(s => s.Order).Select(s => new
                        {
                            s.DocumentId,
                            DocumentTitle = s.Document.Title ?? s.Document.OriginalFileName,
                            DocumentLink = s.Document.DocumentLink,
                            FileName = s.Document.FileName,
                            s.RelevanceScore,
                            s.ChunksUsed
                        })
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving messages: {ex.Message}");
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int conversationId, int messageId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                // Verify conversation exists and belongs to user, and message belongs to conversation
                var message = await _dbContext.Messages
                    .Include(m => m.Conversation)
                    .FirstOrDefaultAsync(m => m.Id == messageId &&
                                              m.ConversationId == conversationId &&
                                              m.Conversation.UserId == userId);

                if (message == null)
                    return NotFound("Message not found");

                _dbContext.Messages.Remove(message);

                // Update conversation's UpdatedAt timestamp
                message.Conversation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the message: {ex.Message}");
            }
        }
    }

    public class AddMessageRequest
    {
        [Required(ErrorMessage = "Role is required")]
        public MessageRole Role { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public required string Content { get; set; }

        public string? Metadata { get; set; }

        /// <summary>
        /// Optional list of document IDs to specifically reference when querying the knowledge base.
        /// If provided, these documents will be included in the context regardless of similarity scores.
        /// </summary>
        public List<int>? ReferencedDocumentIds { get; set; }
    }
}