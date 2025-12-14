using Pgvector;

namespace rag_experiment.Domain
{
    /// <summary>
    /// Represents the source/owner of an embedding to distinguish between user-uploaded documents 
    /// and system knowledge base content.
    /// </summary>
    public enum EmbeddingOwner
    {
        /// <summary>
        /// Embedding generated from a user-uploaded document
        /// </summary>
        UserDocument,

        /// <summary>
        /// Embedding generated from system knowledge base content
        /// </summary>
        SystemKnowledgeBase
    }

    public class Embedding
    {
        public int Id { get; set; }
        public string Text { get; set; }

        /// <summary>
        /// The embedding vector stored as a native PostgreSQL vector type via pgvector.
        /// This enables efficient similarity searches directly in the database.
        /// </summary>
        public Vector EmbeddingData { get; set; }
        public string DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public int ChunkIndex { get; set; }
        public byte[] ChunkHash { get; set; }

        // Source/owner classification
        public EmbeddingOwner Owner { get; set; }

        /// <summary>
        /// The name of the training folder this embedding originated from.
        /// Null for user-uploaded document embeddings, populated for training data embeddings.
        /// </summary>
        public string? TrainingFolderName { get; set; }

        // User association (for access control) - Optional for system knowledge
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Conversation association (for scoping) - Optional
        public int? ConversationId { get; set; }
        public Conversation? Conversation { get; set; }
    }
}

