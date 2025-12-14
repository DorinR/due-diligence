using Microsoft.EntityFrameworkCore;
using rag_experiment.Domain;

namespace rag_experiment.Services
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// The dimensionality of the embedding vectors (OpenAI text-embedding-3-small uses 1536 dimensions)
        /// </summary>
        private const int EmbeddingDimensions = 1536;
        public DbSet<Embedding> Embeddings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageSource> MessageSources { get; set; }
        public DbSet<ConversationCompany> ConversationCompanies { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enable pgvector extension for vector similarity search
            modelBuilder.HasPostgresExtension("vector");

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Conversation entity
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Conversations)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ConversationCompany entity
            modelBuilder.Entity<ConversationCompany>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Companies)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired();
                entity.Property(e => e.OriginalFileName).IsRequired();
                entity.Property(e => e.ContentType).IsRequired();
                entity.Property(e => e.FilePath).IsRequired();
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false); // Make the relationship optional
            });

            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure MessageSource entity
            modelBuilder.Entity<MessageSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.DocumentId).IsRequired();
                entity.Property(e => e.RelevanceScore).IsRequired();
                entity.Property(e => e.ChunksUsed).IsRequired();
                entity.Property(e => e.Order).IsRequired();

                // Configure relationship to Message
                entity.HasOne(e => e.Message)
                    .WithMany(m => m.Sources)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship to Document
                entity.HasOne(e => e.Document)
                    .WithMany(d => d.CitedInMessages)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete documents if message is deleted

                // Create index for efficient queries
                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => e.DocumentId);
            });

            // Configure Embedding entity
            modelBuilder.Entity<Embedding>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();

                // Configure EmbeddingData as a pgvector column with specified dimensions
                entity.Property(e => e.EmbeddingData)
                    .IsRequired()
                    .HasColumnType($"vector({EmbeddingDimensions})");

                entity.Property(e => e.DocumentId).IsRequired();
                entity.Property(e => e.DocumentTitle).IsRequired();
                entity.Property(e => e.ChunkIndex).IsRequired();
                entity.Property(e => e.ChunkHash).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.ConversationId, e.DocumentId, e.ChunkIndex }).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false); // Make the relationship optional
                entity.HasOne(e => e.Conversation)
                    .WithMany()
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false); // Make the relationship optional

                // Add HNSW index for fast approximate nearest neighbor searches using cosine distance
                entity.HasIndex(e => e.EmbeddingData)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
            });
        }
    }
}