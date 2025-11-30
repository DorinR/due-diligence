namespace rag_experiment.Models
{
    /// <summary>
    /// Represents the type of user query intent for adaptive retrieval strategies.
    /// Different intents require different retrieval configurations (top-K, similarity thresholds, etc.)
    /// </summary>
    public enum QueryIntent
    {
        /// <summary>
        /// Regular query mode for standard RAG applications.
        /// Uses typical k value (10-20) with moderate similarity threshold for balanced precision and recall.
        /// Example: "What is the capital of France?", "How does X work?", "Tell me about..."
        /// Strategy: Standard K, balanced similarity threshold
        /// </summary>
        Regular,

        /// <summary>
        /// Exhaustive query mode for queries that require complete coverage.
        /// Uses unlimited k (no limit) with low similarity threshold to ensure 100% recall.
        /// Example: "List all cases", "Find every mention", "Show me all instances", "What are all the..."
        /// Strategy: Unlimited K, low similarity threshold (recall-focused)
        /// </summary>
        Exhaustive
    }

    /// <summary>
    /// Configuration parameters for retrieval based on query intent
    /// </summary>
    public class RetrievalConfig
    {
        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxK { get; set; }

        /// <summary>
        /// Minimum similarity score threshold (0.0 to 1.0)
        /// </summary>
        public float MinSimilarity { get; set; }

        /// <summary>
        /// Human-readable description of this configuration
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Result of query intent classification
    /// </summary>
    public class QueryIntentResult
    {
        /// <summary>
        /// The detected intent of the query
        /// </summary>
        public QueryIntent Intent { get; set; }

        /// <summary>
        /// Explanation of why this intent was chosen (for debugging/transparency)
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// Confidence score if available (0.0 to 1.0)
        /// </summary>
        public float? Confidence { get; set; }
    }
}