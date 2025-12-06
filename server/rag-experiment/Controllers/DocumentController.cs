using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using rag_experiment.Domain;
using rag_experiment.Services;
using rag_experiment.Services.Events;
using rag_experiment.Services.Auth;
using rag_experiment.Services.Query.Models;
using Microsoft.Extensions.Options;

namespace rag_experiment.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserContext _userContext;
        private readonly ITextProcessor _textProcessor;
        private readonly ITextChunker _textChunker;
        private readonly ChunkingSettings _chunkingSettings;

        public DocumentController(
            AppDbContext dbContext,
            IWebHostEnvironment environment,
            IUserContext userContext,
            ITextProcessor textProcessor,
            ITextChunker textChunker,
            IOptions<ChunkingSettings> chunkingSettings)
        {
            _dbContext = dbContext;
            _environment = environment;
            _userContext = userContext;
            _textProcessor = textProcessor;
            _textChunker = textChunker;
            _chunkingSettings = chunkingSettings.Value;
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetDocumentsByConversation(int conversationId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                var conversationExists = await _dbContext.Conversations
                    .AnyAsync(c => c.Id == conversationId && c.UserId == userId);

                if (!conversationExists)
                    return NotFound("Conversation not found or you don't have access to it");

                var documents = await _dbContext.Documents
                    .Where(d => d.ConversationId == conversationId)
                    .Select(d => new
                    {
                        d.Id,
                        d.OriginalFileName,
                        d.ContentType,
                        d.FileSize,
                        d.UploadedAt,
                        d.Description
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving documents: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                var document = await _dbContext.Documents
                    .Include(d => d.Conversation)
                    .FirstOrDefaultAsync(d => d.Id == id && d.Conversation.UserId == userId);

                if (document == null)
                    return NotFound("Document not found or you don't have access to it");

                return Ok(new
                {
                    document.Id,
                    document.OriginalFileName,
                    document.ContentType,
                    document.FileSize,
                    document.UploadedAt,
                    document.Description,
                    document.ConversationId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the document: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                var document = await _dbContext.Documents
                    .Include(d => d.Conversation)
                    .FirstOrDefaultAsync(d => d.Id == id && d.Conversation.UserId == userId);

                if (document == null)
                    return NotFound("Document not found or you don't have access to it");

                if (System.IO.File.Exists(document.FilePath))
                {
                    System.IO.File.Delete(document.FilePath);
                }

                _dbContext.Documents.Remove(document);
                document.Conversation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                EventBus.Publish(new DocumentDeletedEvent(id));

                return Ok(new { message = "Document and associated embeddings deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the document: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();

                var documents = await _dbContext.Documents
                    .Include(d => d.Conversation)
                    .Where(d => d.Conversation.UserId == userId)
                    .Select(d => new
                    {
                        d.Id,
                        d.OriginalFileName,
                        d.ContentType,
                        d.FileSize,
                        d.UploadedAt,
                        d.Description,
                        d.ConversationId,
                        ConversationTitle = d.Conversation.Title
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving documents: {ex.Message}");
            }
        }

        /// <summary>
        /// Estimates the total number of tokens for all .txt files in the token-estimation directory.
        /// </summary>
        [HttpPost("estimate-tokens")]
        [AllowAnonymous]
        public async Task<IActionResult> EstimateTokensForDirectory()
        {
            try
            {
                var tokenEstimationPath = Path.Combine(_environment.ContentRootPath, "Test Data", "token-estimation");

                if (!Directory.Exists(tokenEstimationPath))
                {
                    return BadRequest($"Token estimation directory does not exist: {tokenEstimationPath}");
                }

                var txtFiles = Directory.GetFiles(tokenEstimationPath, "*.txt", SearchOption.TopDirectoryOnly);

                if (!txtFiles.Any())
                {
                    return Ok(new
                    {
                        message = "No .txt files found in the token estimation directory",
                        directoryPath = tokenEstimationPath,
                        totalTokens = 0,
                        totalFiles = 0,
                        files = new object[0]
                    });
                }

                var fileResults = new List<object>();
                var totalTokens = 0;
                var totalChunks = 0;
                var totalCharacters = 0;

                foreach (var filePath in txtFiles)
                {
                    try
                    {
                        var rawText = await System.IO.File.ReadAllTextAsync(filePath);
                        var originalLength = rawText.Length;

                        var processedText = _textProcessor.ProcessText(rawText);
                        var processedLength = processedText.Length;

                        var chunks = _textChunker.ChunkText(processedText);

                        var fileTokens = chunks.Sum(chunk => chunk.Length / 4);
                        var fileChunks = chunks.Count;

                        totalTokens += fileTokens;
                        totalChunks += fileChunks;
                        totalCharacters += processedLength;

                        fileResults.Add(new
                        {
                            fileName = Path.GetFileName(filePath),
                            originalCharacters = originalLength,
                            processedCharacters = processedLength,
                            charactersRemoved = originalLength - processedLength,
                            estimatedTokens = fileTokens,
                            chunkCount = fileChunks,
                            averageChunkSize = fileChunks > 0 ? chunks.Average(c => c.Length) : 0,
                            chunkSizes = chunks.Select(c => c.Length).ToArray()
                        });
                    }
                    catch (Exception fileEx)
                    {
                        fileResults.Add(new
                        {
                            fileName = Path.GetFileName(filePath),
                            error = $"Failed to process file: {fileEx.Message}",
                            estimatedTokens = 0,
                            chunkCount = 0
                        });
                    }
                }

                var syncApiCostUsd = (totalTokens / 1000000.0) * 0.065;
                var batchApiCostUsd = (totalTokens / 1000000.0) * 0.00013;

                var averageTokensPerFile = txtFiles.Length > 0 ? totalTokens / txtFiles.Length : 0;
                var fullCorpusSize = 12000;
                var fullCorpusTotalTokens = averageTokensPerFile * fullCorpusSize;
                var fullCorpusSyncCostUsd = (fullCorpusTotalTokens / 1000000.0) * 0.065;
                var fullCorpusBatchCostUsd = (fullCorpusTotalTokens / 1000000.0) * 0.00013;

                return Ok(new
                {
                    message = "Token estimation completed successfully",
                    directoryPath = tokenEstimationPath,
                    summary = new
                    {
                        totalFiles = txtFiles.Length,
                        totalCharacters = totalCharacters,
                        totalTokens = totalTokens,
                        totalChunks = totalChunks,
                        averageTokensPerFile = txtFiles.Length > 0 ? totalTokens / txtFiles.Length : 0,
                        averageChunksPerFile = txtFiles.Length > 0 ? (double)totalChunks / txtFiles.Length : 0,
                        pricing = new
                        {
                            sampleFiles = new
                            {
                                syncApiCostUsd = Math.Round(syncApiCostUsd, 6),
                                batchApiCostUsd = Math.Round(batchApiCostUsd, 6),
                                syncApiRate = "$0.065 per 1M tokens",
                                batchApiRate = "$0.00013 per 1M tokens",
                                potentialSavings = Math.Round(syncApiCostUsd - batchApiCostUsd, 6),
                                savingsPercentage = syncApiCostUsd > 0
                                    ? Math.Round(((syncApiCostUsd - batchApiCostUsd) / syncApiCostUsd) * 100, 2)
                                    : 0
                            },
                            fullCorpusEstimate = new
                            {
                                totalFiles = fullCorpusSize,
                                averageTokensPerFile = averageTokensPerFile,
                                estimatedTotalTokens = fullCorpusTotalTokens,
                                syncApiCostUsd = Math.Round(fullCorpusSyncCostUsd, 2),
                                batchApiCostUsd = Math.Round(fullCorpusBatchCostUsd, 2),
                                potentialSavings = Math.Round(fullCorpusSyncCostUsd - fullCorpusBatchCostUsd, 2),
                                savingsPercentage = fullCorpusSyncCostUsd > 0
                                    ? Math.Round(
                                        ((fullCorpusSyncCostUsd - fullCorpusBatchCostUsd) / fullCorpusSyncCostUsd) *
                                        100, 2)
                                    : 0,
                                note = "Costs estimated based on average tokens per file from sample"
                            }
                        },
                        processingSettings = new
                        {
                            chunkSize = _chunkingSettings.ChunkSize,
                            chunkOverlap = _chunkingSettings.ChunkOverlap,
                            tokenEstimationMethod = "1 token ≈ 4 characters (OpenAI standard)"
                        }
                    },
                    files = fileResults
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while estimating tokens: {ex.Message}");
            }
        }
    }
}
