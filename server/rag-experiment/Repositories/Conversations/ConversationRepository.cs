using Microsoft.EntityFrameworkCore;
using rag_experiment.Domain;
using rag_experiment.Services;
using rag_experiment.Services.Auth;
using rag_experiment.Services.BackgroundJobs.Models;

namespace rag_experiment.Repositories.Conversations
{
    /// <summary>
    /// Repository implementation for conversation operations using Entity Framework
    /// </summary>
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IUserContext _userContext;

        public ConversationRepository(AppDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext;
            _userContext = userContext;
        }

        /// <summary>
        /// Retrieves all messages for a specific conversation with their source citations
        /// </summary>
        /// <param name="conversationId">The ID of the conversation</param>
        /// <returns>List of messages with sources ordered by timestamp, or empty list if conversation not found</returns>
        public async Task<List<Message>> GetMessagesAsync(int conversationId)
        {
            var userId = _userContext.GetCurrentUserId();

            // First verify that the conversation exists and belongs to the current user
            var conversationExists = await _dbContext.Conversations
                .AnyAsync(c => c.Id == conversationId && c.UserId == userId);

            if (!conversationExists)
                return new List<Message>();

            // Get all messages for the conversation with their sources, ordered by timestamp
            var messages = await _dbContext.Messages
                .Include(m => m.Sources)
                .ThenInclude(s => s.Document)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        /// <summary>
        /// Adds a new message to a conversation
        /// </summary>
        /// <param name="message">The message to add</param>
        /// <returns>The added message with its generated ID</returns>
        public async Task<Message> AddMessageAsync(Message message)
        {
            await _dbContext.Messages.AddAsync(message);
            await _dbContext.SaveChangesAsync();
            return message;
        }

        /// <summary>
        /// Updates the ingestion status on a conversation.
        /// </summary>
        /// <param name="conversationId">The conversation to update.</param>
        /// <param name="status">The new ingestion status.</param>
        /// <returns>True if the conversation was found and updated, false otherwise.</returns>
        public async Task<bool> UpdateIngestionStatusAsync(int conversationId, BatchProcessingStatus status)
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return false;

            conversation.IngestionStatus = status;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}