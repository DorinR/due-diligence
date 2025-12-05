using System.ComponentModel.DataAnnotations;

namespace rag_experiment.Domain
{
    /// <summary>
    /// Represents a company that is being researched within a conversation.
    /// A conversation can have multiple companies associated with it.
    /// </summary>
    public class ConversationCompany
    {
        public int Id { get; set; }

        /// <summary>
        /// The name of the company being researched
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; }

        /// <summary>
        /// The conversation this company is associated with
        /// </summary>
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }
    }
}

