using Microsoft.AspNetCore.SignalR;
using rag_experiment.Hubs.Models;

namespace rag_experiment.Hubs.Services;

/// <summary>
/// Service interface for sending document processing notifications via SignalR.
/// Background jobs inject this service to broadcast real-time updates to subscribed clients.
/// </summary>
public interface IDocumentProcessingNotifier
{
    /// <summary>
    /// Send a progress update to all clients subscribed to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to send the update to</param>
    /// <param name="update">The progress update data</param>
    Task SendProgressUpdateAsync(string conversationId, DocumentProcessingUpdate update);

    /// <summary>
    /// Send a completion notification to all clients subscribed to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to send the notification to</param>
    /// <param name="result">The completion result data</param>
    Task SendCompletionAsync(string conversationId, ProcessingCompleteResult result);

    /// <summary>
    /// Send an error notification to all clients subscribed to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to send the notification to</param>
    /// <param name="error">The error data</param>
    Task SendErrorAsync(string conversationId, ProcessingErrorResult error);
}

/// <summary>
/// Implementation of document processing notifier using SignalR.
/// Wraps IHubContext to provide a clean, testable interface for background jobs.
/// </summary>
public class DocumentProcessingNotifier : IDocumentProcessingNotifier
{
    private readonly IHubContext<DocumentProcessingHub> _hubContext;
    private readonly ILogger<DocumentProcessingNotifier> _logger;

    public DocumentProcessingNotifier(
        IHubContext<DocumentProcessingHub> hubContext,
        ILogger<DocumentProcessingNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendProgressUpdateAsync(string conversationId, DocumentProcessingUpdate update)
    {
        _logger.LogInformation(
            "Sending progress update to conversation {ConversationId}: Stage={Stage}, Progress={Progress}%",
            conversationId, update.Stage, update.ProgressPercent);

        await _hubContext.Clients
            .Group(conversationId)
            .SendAsync("ReceiveProcessingUpdate", update);
    }

    /// <inheritdoc />
    public async Task SendCompletionAsync(string conversationId, ProcessingCompleteResult result)
    {
        _logger.LogInformation(
            "Sending completion notification to conversation {ConversationId}: Success={Success}, Failed={Failed}",
            conversationId, result.SuccessfulDocuments, result.FailedDocuments);

        await _hubContext.Clients
            .Group(conversationId)
            .SendAsync("ReceiveProcessingComplete", result);
    }

    /// <inheritdoc />
    public async Task SendErrorAsync(string conversationId, ProcessingErrorResult error)
    {
        _logger.LogWarning(
            "Sending error notification to conversation {ConversationId}: Stage={Stage}, Error={Error}",
            conversationId, error.Stage, error.ErrorMessage);

        await _hubContext.Clients
            .Group(conversationId)
            .SendAsync("ReceiveProcessingError", error);
    }
}
