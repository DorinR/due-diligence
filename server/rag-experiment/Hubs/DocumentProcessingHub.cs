using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace rag_experiment.Hubs;

/// <summary>
/// SignalR hub for real-time document processing status updates.
/// Manages client subscriptions to conversation-specific processing updates.
/// Clients connect to this hub to receive live progress updates during document processing.
/// </summary>
public class DocumentProcessingHub : Hub
{
    private readonly ILogger<DocumentProcessingHub> _logger;

    public DocumentProcessingHub(ILogger<DocumentProcessingHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        _logger.LogInformation(
            "Client connected to DocumentProcessingHub. ConnectionId: {ConnectionId}, User: {UserId}",
            Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// SignalR automatically removes connections from all groups on disconnect.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name;

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Client disconnected from DocumentProcessingHub with error. ConnectionId: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, userId);
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected from DocumentProcessingHub. ConnectionId: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe client to real-time processing updates for a specific conversation.
    /// Clients call this method when they open a conversation page to receive live updates.
    /// </summary>
    /// <param name="conversationId">The conversation ID to subscribe to</param>
    public async Task SubscribeToConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

        _logger.LogInformation(
            "Client subscribed to conversation. ConnectionId: {ConnectionId}, ConversationId: {ConversationId}",
            Context.ConnectionId, conversationId);

        // Send confirmation back to the caller
        await Clients.Caller.SendAsync("SubscriptionConfirmed", conversationId);
    }

    /// <summary>
    /// Unsubscribe client from processing updates for a specific conversation.
    /// Clients call this when leaving a conversation page or component unmounting.
    /// </summary>
    /// <param name="conversationId">The conversation ID to unsubscribe from</param>
    public async Task UnsubscribeFromConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

        _logger.LogInformation(
            "Client unsubscribed from conversation. ConnectionId: {ConnectionId}, ConversationId: {ConversationId}",
            Context.ConnectionId, conversationId);
    }
}
