using System.ComponentModel.DataAnnotations;

namespace rag_experiment.Domain
{
    /// <summary>
    /// Represents a research conversation for performing due diligence on one or more companies
    /// </summary>
    public class Conversation
    {
        public int Id { get; set; }

        [Required] [MaxLength(200)] public string Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // User association
        public int UserId { get; set; }
        public User User { get; set; }

        // Navigation properties
        public List<Document> Documents { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
        
        /// <summary>
        /// The companies being researched in this conversation
        /// </summary>
        public List<ConversationCompany> Companies { get; set; } = new();
    }
}

