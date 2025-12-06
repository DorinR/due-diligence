using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using rag_experiment.Domain;
using rag_experiment.Services;
using rag_experiment.Services.Auth;
using rag_experiment.Services.BackgroundJobs;
using rag_experiment.Services.BackgroundJobs.Models;

namespace rag_experiment.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly IDocumentProcessingJobService _documentProcessingJobService;
    private readonly FilingIngestionOptions _filingOptions;

    public ConversationController(
        AppDbContext dbContext,
        IUserContext userContext,
        IDocumentProcessingJobService documentProcessingJobService,
        IOptions<FilingIngestionOptions> filingOptions)
    {
        _dbContext = dbContext;
        _userContext = userContext;
        _documentProcessingJobService = documentProcessingJobService;
        _filingOptions = filingOptions.Value;
    }

    /// <summary>
    /// Creates a new research conversation for the specified companies
    /// </summary>
    /// <param name="request">Request containing the list of company names to research</param>
    /// <returns>The created conversation with associated companies</returns>
    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            if (request.CompanyNames == null || request.CompanyNames.Count == 0)
                return BadRequest("At least one company name is required");

            var userId = _userContext.GetCurrentUserId();

            // Generate a title from the company names
            var title = request.CompanyNames.Count == 1
                ? $"Research: {request.CompanyNames[0]}"
                : $"Research: {string.Join(", ", request.CompanyNames.Take(3))}" +
                  (request.CompanyNames.Count > 3 ? $" (+{request.CompanyNames.Count - 3} more)" : "");

            var conversation = new Conversation
            {
                Title = title,
                UserId = userId,
                Companies = request.CompanyNames.Select(name => new ConversationCompany
                {
                    CompanyName = name
                }).ToList()
            };

            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                id = conversation.Id,
                title = conversation.Title,
                companies = conversation.Companies.Select(c => new { c.Id, c.CompanyName }),
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating the conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets (overwrites) the company for an existing conversation and starts ingestion.
    /// </summary>
    [HttpPost("company")]
    public async Task<IActionResult> SetConversationCompany([FromBody] SetConversationCompanyRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CompanyName))
                return BadRequest("Company name is required");

            if (request.ConversationId <= 0)
                return BadRequest("conversationId must be greater than zero");

            var userId = _userContext.GetCurrentUserId();

            var conversation = await _dbContext.Conversations
                .Include(c => c.Companies)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == userId);

            if (conversation == null)
                return NotFound("Conversation not found");

            conversation.Companies.Clear();
            conversation.Companies.Add(new ConversationCompany { CompanyName = request.CompanyName });
            conversation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var filingTypes = _filingOptions.DefaultFilingTypes ?? new List<string>();
            if (filingTypes.Count == 0)
                return StatusCode(500, "No default filing types configured for ingestion");

            await _documentProcessingJobService.SetupFilingIngestionPipeline(
                request.CompanyName,
                filingTypes,
                userId,
                conversation.Id);

            return Ok(new
            {
                conversation.Id,
                conversation.Title,
                Companies = conversation.Companies.Select(c => new { c.Id, c.CompanyName }),
                conversation.CreatedAt,
                conversation.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while setting the conversation company: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllConversations()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();

            var conversations = await _dbContext.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    Companies = c.Companies.Select(cc => new { cc.Id, cc.CompanyName }),
                    c.CreatedAt,
                    c.UpdatedAt,
                    DocumentCount = c.Documents.Count,
                    MessageCount = c.Messages.Count
                })
                .ToListAsync();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving conversations: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConversation(int id)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();

            var conversation = await _dbContext.Conversations
                .Include(c => c.Companies)
                .Include(c => c.Documents)
                .Include(c => c.Messages)
                .ThenInclude(m => m.Sources)
                .ThenInclude(s => s.Document)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
                return NotFound("Conversation not found");

            return Ok(new
            {
                conversation.Id,
                conversation.Title,
                Companies = conversation.Companies.Select(c => new { c.Id, c.CompanyName }),
                conversation.CreatedAt,
                conversation.UpdatedAt,
                Documents = conversation.Documents.Select(d => new
                {
                    d.Id,
                    d.OriginalFileName,
                    d.ContentType,
                    d.FileSize,
                    d.UploadedAt,
                    d.Description
                }),
                Messages = conversation.Messages.OrderBy(m => m.Timestamp).Select(m => new
                {
                    m.Id,
                    m.Role,
                    m.Content,
                    m.Timestamp,
                    m.Metadata,
                    Sources = m.Sources.OrderBy(s => s.Order).Select(s => new
                    {
                        s.DocumentId,
                        DocumentTitle = s.Document.Title,
                        FileName = s.Document.FileName,
                        s.RelevanceScore,
                        s.ChunksUsed
                    })
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving the conversation: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConversation(int id, [FromBody] UpdateConversationRequest request)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();

            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
                return NotFound("Conversation not found");

            conversation.Title = request.Title;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt,
                conversation.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while updating the conversation: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();

            var conversation = await _dbContext.Conversations
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null)
                return NotFound("Conversation not found");

            // Delete physical files
            foreach (var document in conversation.Documents)
                if (System.IO.File.Exists(document.FilePath))
                    System.IO.File.Delete(document.FilePath);

            // EF Core will handle cascade deletes for Documents, Messages, and Embeddings
            _dbContext.Conversations.Remove(conversation);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Conversation and all associated data deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while deleting the conversation: {ex.Message}");
        }
    }
}

/// <summary>
/// Request to create a new research conversation
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// List of company names to research in this conversation
    /// </summary>
    public List<string> CompanyNames { get; set; } = new();
}

public class UpdateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Request to set the company for an existing conversation.
/// </summary>
public class SetConversationCompanyRequest
{
    /// <summary>
    /// Conversation to update.
    /// </summary>
    public int ConversationId { get; set; }

    /// <summary>
    /// Company name to associate with the conversation.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;
}