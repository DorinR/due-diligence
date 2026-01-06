using rag_experiment.Domain;
using rag_experiment.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace rag_experiment.Services.Ingestion.VectorStorage
{
    public class EmbeddingRepository : IEmbeddingRepository
    {
        private readonly AppDbContext _context;
        private readonly IUserContext _userContext;

        public EmbeddingRepository(AppDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public void AddEmbedding(string text, float[] embeddingData, string documentId, int? userId,
            int? conversationId, string documentTitle, EmbeddingOwner owner, int chunkIndex, byte[] chunkHash,
            string? trainingFolderName = null)
        {
            var embedding = new Embedding
            {
                Text = text,
                EmbeddingData = new Vector(embeddingData),
                DocumentId = documentId,
                DocumentTitle = documentTitle,
                Owner = owner,
                UserId = userId,
                ConversationId = conversationId,
                ChunkIndex = chunkIndex,
                ChunkHash = chunkHash,
                TrainingFolderName = trainingFolderName
            };

            _context.Embeddings.Add(embedding);
            _context.SaveChanges();
        }

        public (int Id, string Text, float[] EmbeddingVector, string DocumentId, string DocumentTitle)
            GetEmbedding(int id)
        {
            var userId = _userContext.GetCurrentUserId();

            var embedding = _context.Embeddings
                .FirstOrDefault(e => e.Id == id && e.UserId == userId);

            if (embedding == null)
                return default;

            return (embedding.Id, embedding.Text, embedding.EmbeddingData.ToArray(), embedding.DocumentId,
                embedding.DocumentTitle);
        }

        public void UpdateEmbedding(int id, string newText, float[] newEmbeddingData, string? documentId = null,
            string? documentTitle = null, EmbeddingOwner? owner = null)
        {
            var userId = _userContext.GetCurrentUserId();

            var embedding = _context.Embeddings
                .FirstOrDefault(e => e.Id == id && e.UserId == userId);

            if (embedding != null)
            {
                embedding.Text = newText;
                embedding.EmbeddingData = new Vector(newEmbeddingData);

                if (documentId != null)
                {
                    embedding.DocumentId = documentId;
                }

                if (documentTitle != null)
                {
                    embedding.DocumentTitle = documentTitle;
                }

                if (owner.HasValue)
                {
                    embedding.Owner = owner.Value;
                }

                _context.SaveChanges();
            }
        }

        public void DeleteEmbedding(int id)
        {
            var userId = _userContext.GetCurrentUserId();

            var embedding = _context.Embeddings
                .FirstOrDefault(e => e.Id == id && e.UserId == userId);

            if (embedding != null)
            {
                _context.Embeddings.Remove(embedding);
                _context.SaveChanges();
            }
        }

        public void DeleteEmbeddingsByDocumentId(string documentId)
        {
            var userId = _userContext.GetCurrentUserId();

            var embeddingsToDelete = _context.Embeddings
                .Where(e => e.DocumentId == documentId && e.UserId == userId)
                .ToList();

            if (embeddingsToDelete.Any())
            {
                _context.Embeddings.RemoveRange(embeddingsToDelete);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Finds the most similar embeddings in the database to the query embedding, scoped to a conversation.
        /// Uses pgvector's native cosine distance for efficient similarity search.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="conversationId">The conversation ID to scope the search to</param>
        /// <param name="topK">Number of results to return</param>
        /// <returns>List of text chunks, document IDs, document titles, and their similarity scores, ordered by similarity</returns>
        public List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>
            FindSimilarEmbeddingsFromUsersDocuments(float[] queryEmbedding, int? conversationId, int topK = 10)
        {
            var userId = _userContext.GetCurrentUserId();
            var queryVector = new Vector(queryEmbedding);

            // Use pgvector's native cosine distance operator for efficient similarity search
            // Cosine distance = 1 - cosine similarity, so we convert back to similarity
            var results = _context.Embeddings
                .Where(e => e.UserId == userId &&
                            (conversationId == null || e.ConversationId == conversationId) &&
                            e.Owner == EmbeddingOwner.UserDocument)
                .OrderBy(e => e.EmbeddingData.CosineDistance(queryVector))
                .Take(topK)
                .Select(e => new
                {
                    e.Text,
                    e.DocumentId,
                    e.DocumentTitle,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToList();

            // Convert cosine distance to similarity (similarity = 1 - distance)
            return results
                .Select(r => (r.Text, r.DocumentId, r.DocumentTitle, Similarity: 1f - (float)r.Distance))
                .ToList();
        }

        /// <summary>
        /// Finds the most similar embeddings in the database to the query embedding across all user's conversations.
        /// Uses pgvector's native cosine distance for efficient similarity search.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="topK">Number of results to return</param>
        /// <returns>List of text chunks, document IDs, document titles, and their similarity scores, ordered by similarity</returns>
        public List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>
            FindSimilarEmbeddingsAllConversations(float[] queryEmbedding, int topK = 10)
        {
            var userId = _userContext.GetCurrentUserId();
            var queryVector = new Vector(queryEmbedding);

            // Use pgvector's native cosine distance operator for efficient similarity search
            var results = _context.Embeddings
                .Where(e => e.UserId == userId && e.Owner == EmbeddingOwner.UserDocument)
                .OrderBy(e => e.EmbeddingData.CosineDistance(queryVector))
                .Take(topK)
                .Select(e => new
                {
                    e.Text,
                    e.DocumentId,
                    e.DocumentTitle,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToList();

            // Convert cosine distance to similarity (similarity = 1 - distance)
            return results
                .Select(r => (r.Text, r.DocumentId, r.DocumentTitle, Similarity: 1f - (float)r.Distance))
                .ToList();
        }

        /// <summary>
        /// Finds the most similar embeddings in the database to the query embedding across ALL embeddings in the entire system.
        /// This searches through all users' documents and conversations without any filtering.
        /// Uses pgvector's native cosine distance for efficient similarity search.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="topK">Number of results to return</param>
        /// <returns>Task containing a list of text chunks, document IDs, document titles, and their similarity scores, ordered by similarity</returns>
        public async Task<List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>>
            FindSimilarEmbeddingsAsync(float[] queryEmbedding, int topK = 10)
        {
            var queryVector = new Vector(queryEmbedding);

            // Use pgvector's native cosine distance operator for efficient similarity search
            var results = await _context.Embeddings
                .Where(e => e.Owner == EmbeddingOwner.UserDocument)
                .OrderBy(e => e.EmbeddingData.CosineDistance(queryVector))
                .Take(topK)
                .Select(e => new
                {
                    e.Text,
                    e.DocumentId,
                    e.DocumentTitle,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToListAsync();

            // Convert cosine distance to similarity (similarity = 1 - distance)
            return results
                .Select(r => (r.Text, r.DocumentId, r.DocumentTitle, Similarity: 1f - (float)r.Distance))
                .ToList();
        }

        /// <summary>
        /// Adaptive retrieval method that finds similar embeddings using both a similarity threshold and maxK limit.
        /// Returns all results above the threshold, up to maxK results.
        /// This enables flexible retrieval strategies based on query intent.
        /// Uses pgvector's native cosine distance for efficient similarity search.
        /// When maxK is int.MaxValue, returns all matching results (no limit).
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="maxK">Maximum number of results to return. Use int.MaxValue for unlimited results.</param>
        /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
        /// <returns>Task containing a list of text chunks, document IDs, document titles, and their similarity scores</returns>
        public async Task<List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>>
            FindSimilarEmbeddingsAdaptiveAsync(
                float[] queryEmbedding,
                int maxK = 10,
                float minSimilarity = 0.70f)
        {
            var queryVector = new Vector(queryEmbedding);

            // Convert similarity threshold to distance threshold (distance = 1 - similarity)
            var maxDistance = 1.0 - minSimilarity;

            // DEBUG: Calculate cosine distance for ALL embeddings to diagnose threshold issues
            var allEmbeddings = await _context.Embeddings
                .Where(e => e.Owner == EmbeddingOwner.UserDocument)
                .Select(e => new
                {
                    e.Id,
                    e.DocumentTitle,
                    e.ChunkIndex,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToListAsync();

            Console.WriteLine($"=== DEBUG: Cosine Distance Analysis ===");
            Console.WriteLine($"Total embeddings in SystemKnowledgeBase: {allEmbeddings.Count}");
            Console.WriteLine($"Max distance threshold (minSimilarity={minSimilarity}): {maxDistance}");
            Console.WriteLine($"--- All distances (sorted by distance asc) ---");

            foreach (var emb in allEmbeddings.OrderBy(e => e.Distance).Take(50)) // Show top 50 closest
            {
                var similarity = 1.0 - emb.Distance;
                var passesThreshold = emb.Distance <= maxDistance ? "✓ PASS" : "✗ FAIL";
                Console.WriteLine($"  ID={emb.Id}, Doc='{emb.DocumentTitle}', Chunk={emb.ChunkIndex}, Distance={emb.Distance:F4}, Similarity={similarity:F4} {passesThreshold}");
            }

            var passingCount = allEmbeddings.Count(e => e.Distance <= maxDistance);
            Console.WriteLine($"--- Summary: {passingCount}/{allEmbeddings.Count} embeddings pass the threshold ---");
            Console.WriteLine($"=== END DEBUG ===");

            // Build the query with pgvector's native cosine distance
            var query = _context.Embeddings
                .Where(e => e.Owner == EmbeddingOwner.UserDocument)
                .Where(e => e.EmbeddingData.CosineDistance(queryVector) <= maxDistance)
                .OrderBy(e => e.EmbeddingData.CosineDistance(queryVector));

            // Apply limit if not unlimited
            var limitedQuery = maxK == int.MaxValue
                ? query
                : query.Take(maxK);

            var results = await limitedQuery
                .Select(e => new
                {
                    e.Text,
                    e.DocumentId,
                    e.DocumentTitle,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToListAsync();

            // Convert cosine distance to similarity (similarity = 1 - distance)
            return results
                .Select(r => (r.Text, r.DocumentId, r.DocumentTitle, Similarity: 1f - (float)r.Distance))
                .ToList();
        }

        /// <summary>
        /// Adaptive retrieval for user's documents scoped to a conversation.
        /// Uses both similarity threshold and maxK limit for flexible retrieval.
        /// Uses pgvector's native cosine distance for efficient similarity search.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="conversationId">The conversation ID to scope the search to</param>
        /// <param name="maxK">Maximum number of results to return</param>
        /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
        /// <returns>List of text chunks, document IDs, document titles, and their similarity scores</returns>
        public List<(string Text, string DocumentId, string DocumentTitle, float Similarity)>
            FindSimilarEmbeddingsFromUsersDocumentsAdaptive(
                float[] queryEmbedding,
                int? conversationId,
                int maxK = 10,
                float minSimilarity = 0.70f)
        {
            var userId = _userContext.GetCurrentUserId();
            var queryVector = new Vector(queryEmbedding);

            // Convert similarity threshold to distance threshold (distance = 1 - similarity)
            var maxDistance = 1.0 - minSimilarity;

            // Use pgvector's native cosine distance operator for efficient similarity search
            var results = _context.Embeddings
                .Where(e => e.UserId == userId &&
                            (conversationId == null || e.ConversationId == conversationId) &&
                            e.Owner == EmbeddingOwner.UserDocument)
                .Where(e => e.EmbeddingData.CosineDistance(queryVector) <= maxDistance)
                .OrderBy(e => e.EmbeddingData.CosineDistance(queryVector))
                .Take(maxK)
                .Select(e => new
                {
                    e.Text,
                    e.DocumentId,
                    e.DocumentTitle,
                    Distance = e.EmbeddingData.CosineDistance(queryVector)
                })
                .ToList();

            // Convert cosine distance to similarity (similarity = 1 - distance)
            return results
                .Select(r => (r.Text, r.DocumentId, r.DocumentTitle, Similarity: 1f - (float)r.Distance))
                .ToList();
        }

        public async Task UpsertEmbeddingsAsync(IEnumerable<EmbeddingUpsertItem> items,
            CancellationToken cancellationToken = default)
        {
            // Group by scope to minimize queries
            var itemsByScope = items
                .GroupBy(i => new { i.UserId, i.ConversationId, i.DocumentId })
                .ToList();

            foreach (var scopeGroup in itemsByScope)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var scope = scopeGroup.Key;

                // Prefetch existing rows for this scope
                var existing = await _context.Embeddings
                    .Where(e => (scope.UserId == null ? e.UserId == null : e.UserId == scope.UserId)
                                && (scope.ConversationId == null
                                    ? e.ConversationId == null
                                    : e.ConversationId == scope.ConversationId)
                                && e.DocumentId == scope.DocumentId)
                    .Select(e => new { e.ChunkIndex, e.Id, e.ChunkHash })
                    .ToListAsync(cancellationToken);

                var existingByIndex = existing.ToDictionary(x => x.ChunkIndex, x => x);

                var toInsert = new List<Embedding>();
                foreach (var item in scopeGroup)
                {
                    if (!existingByIndex.TryGetValue(item.ChunkIndex, out var existingRow))
                    {
                        toInsert.Add(new Embedding
                        {
                            Text = item.Text,
                            EmbeddingData = new Vector(item.Vector),
                            DocumentId = item.DocumentId,
                            DocumentTitle = item.DocumentTitle ?? string.Empty,
                            Owner = item.Owner,
                            UserId = item.UserId,
                            ConversationId = item.ConversationId,
                            ChunkIndex = item.ChunkIndex,
                            ChunkHash = item.ChunkHash,
                            TrainingFolderName = item.TrainingFolderName
                        });
                    }
                    else
                    {
                        // Update only if content changed (hash differs)
                        if (existingRow.ChunkHash == null || !item.ChunkHash.SequenceEqual(existingRow.ChunkHash))
                        {
                            var entity =
                                await _context.Embeddings.FirstAsync(e => e.Id == existingRow.Id, cancellationToken);
                            entity.Text = item.Text;
                            entity.EmbeddingData = new Vector(item.Vector);
                            entity.DocumentTitle = item.DocumentTitle ?? entity.DocumentTitle;
                            entity.Owner = item.Owner;
                            entity.ChunkHash = item.ChunkHash;
                            entity.TrainingFolderName = item.TrainingFolderName;
                        }
                    }
                }

                if (toInsert.Count > 0)
                {
                    await _context.Embeddings.AddRangeAsync(toInsert, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Upserts a batch of document-only embeddings for arbitrary datasets.
        /// This method is optimized for bulk operations and uses DocumentId + ChunkIndex as the uniqueness key.
        /// It performs efficient bulk inserts and updates only when content has changed (based on chunk hash).
        /// </summary>
        /// <param name="items">The embedding items to upsert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when the batch upsert finishes</returns>
        public async Task UpsertDocumentEmbeddingsAsync(IEnumerable<EmbeddingUpsertItem> items,
            CancellationToken cancellationToken = default)
        {
            // Group by DocumentId to minimize queries and optimize batching
            var itemsByDocument = items
                .GroupBy(i => i.DocumentId)
                .ToList();

            foreach (var documentGroup in itemsByDocument)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentId = documentGroup.Key;

                // Prefetch existing embeddings for this document
                var existing = await _context.Embeddings
                    .Where(e => e.DocumentId == documentId)
                    .Select(e => new { e.ChunkIndex, e.Id, e.ChunkHash })
                    .ToListAsync(cancellationToken);

                var existingByIndex = existing.ToDictionary(x => x.ChunkIndex, x => x);

                var toInsert = new List<Embedding>();
                foreach (var item in documentGroup)
                {
                    if (!existingByIndex.TryGetValue(item.ChunkIndex, out var existingRow))
                    {
                        // New embedding - add to insert batch
                        toInsert.Add(new Embedding
                        {
                            Text = item.Text,
                            EmbeddingData = new Vector(item.Vector),
                            DocumentId = item.DocumentId,
                            DocumentTitle = item.DocumentTitle ?? string.Empty,
                            Owner = item.Owner,
                            UserId = item.UserId, // Typically null for document-only embeddings
                            ConversationId = item.ConversationId, // Typically null for document-only embeddings
                            ChunkIndex = item.ChunkIndex,
                            ChunkHash = item.ChunkHash,
                            TrainingFolderName = item.TrainingFolderName
                        });
                    }
                    else
                    {
                        // Update only if content changed (hash differs)
                        if (existingRow.ChunkHash == null || !item.ChunkHash.SequenceEqual(existingRow.ChunkHash))
                        {
                            var entity =
                                await _context.Embeddings.FirstAsync(e => e.Id == existingRow.Id, cancellationToken);
                            entity.Text = item.Text;
                            entity.EmbeddingData = new Vector(item.Vector);
                            entity.DocumentTitle = item.DocumentTitle ?? entity.DocumentTitle;
                            entity.Owner = item.Owner;
                            entity.ChunkHash = item.ChunkHash;
                            entity.TrainingFolderName = item.TrainingFolderName;
                            // Note: UserId and ConversationId are typically not updated for document-only embeddings
                        }
                    }
                }

                // Bulk insert new embeddings for this document
                if (toInsert.Count > 0)
                {
                    await _context.Embeddings.AddRangeAsync(toInsert, cancellationToken);
                }
            }

            // Single save operation for all changes
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}