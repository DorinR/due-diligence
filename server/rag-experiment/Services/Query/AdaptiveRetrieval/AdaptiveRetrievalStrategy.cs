using rag_experiment.Services.Query.Models;

namespace rag_experiment.Services.Query
{
    /// <summary>
    /// Implementation of adaptive retrieval strategy that maps query intents
    /// to optimal retrieval configurations (maxK and similarity thresholds)
    /// </summary>
    public class AdaptiveRetrievalStrategy : IAdaptiveRetrievalStrategy
    {
        private readonly ILogger<AdaptiveRetrievalStrategy> _logger;
        private readonly IConfiguration _configuration;

        public AdaptiveRetrievalStrategy(
            ILogger<AdaptiveRetrievalStrategy> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public RetrievalConfig GetConfigForIntent(QueryIntent intent, string? query = null)
        {
            // Base configurations for each intent type
            var config = intent switch
            {
                QueryIntent.Regular => new RetrievalConfig
                {
                    MaxK = GetConfigValue("RetrievalConfig:Regular:MaxK", 15),
                    MinSimilarity = GetConfigValue("RetrievalConfig:Regular:MinSimilarity", 0.70f),
                    Description = "Standard RAG: Balanced precision and recall"
                },

                QueryIntent.Exhaustive => new RetrievalConfig
                {
                    MaxK = GetConfigValue("RetrievalConfig:Exhaustive:MaxK", int.MaxValue),
                    MinSimilarity = GetConfigValue("RetrievalConfig:Exhaustive:MinSimilarity", 0.0f),
                    Description = "Exhaustive search: Maximum recall, no k limit"
                },

                _ => new RetrievalConfig
                {
                    MaxK = 15,
                    MinSimilarity = 0.70f,
                    Description = "Default configuration (Regular mode)"
                }
            };

            _logger.LogInformation(
                "Selected retrieval config for {Intent}: MaxK={MaxK}, MinSimilarity={MinSimilarity}",
                intent, config.MaxK, config.MinSimilarity);

            return config;
        }

        /// <summary>
        /// Gets configuration value with fallback to default
        /// </summary>
        private T GetConfigValue<T>(string key, T defaultValue)
        {
            try
            {
                var value = _configuration.GetValue<T>(key);
                return value ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}