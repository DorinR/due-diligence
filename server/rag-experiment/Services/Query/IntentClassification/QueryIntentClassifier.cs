using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using rag_experiment.Models;

namespace rag_experiment.Services.Query
{
    /// <summary>
    /// LLM-based query intent classifier that analyzes user queries to determine
    /// the appropriate retrieval strategy (regular or exhaustive)
    /// </summary>
    public class QueryIntentClassifier : IQueryIntentClassifier
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _openAiModel;
        private readonly ILogger<QueryIntentClassifier> _logger;

        public QueryIntentClassifier(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<QueryIntentClassifier> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"]
                      ?? throw new ArgumentException("OpenAI API key not found in configuration");
            _openAiModel = configuration["OpenAI:ChatModel"] ?? "gpt-3.5-turbo";
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<QueryIntentResult> ClassifyQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new QueryIntentResult
                {
                    Intent = QueryIntent.Regular,
                    Reasoning = "Empty query defaulted to regular intent"
                };
            }

            try
            {
                var systemPrompt =
                    @"You are an expert at analyzing user queries to determine their intent for information retrieval.

Classify the query into ONE of these categories:

1. REGULAR: Standard queries that seek specific information, explanations, examples, or general exploration.
   Examples: ""What is X?"", ""How does Y work?"", ""Tell me about..."", ""What are some examples of..."", ""Compare X and Y""
   This is the default mode for most queries.

2. EXHAUSTIVE: Queries that explicitly or implicitly want ALL instances, a complete list, or exhaustive coverage.
   Examples: ""List all cases"", ""Find every mention"", ""Show me all instances"", ""What are all the..."", ""Give me every...""
   Keywords: ""all"", ""every"", ""complete"", ""exhaustive"", ""list all"", ""entire"", ""each""
   Use EXHAUSTIVE only when the query clearly indicates the user wants comprehensive/complete coverage.

Respond ONLY with valid JSON in this exact format:
{
  ""intent"": ""REGULAR"" | ""EXHAUSTIVE"",
  ""reasoning"": ""brief explanation of why this intent was chosen""
}";

                var chatMessage = new
                {
                    model = _openAiModel,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = $"Classify this query: \"{query}\"" }
                    },
                    temperature = 0.1, // Low temperature for consistent classification
                    max_tokens = 150
                };

                var jsonContent = JsonSerializer.Serialize(chatMessage);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent);

                var generatedResponse = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

                if (string.IsNullOrEmpty(generatedResponse))
                {
                    _logger.LogWarning("Empty response from LLM for intent classification");
                    return FallbackClassification(query);
                }

                // Parse the JSON response
                var intentResponse = JsonSerializer.Deserialize<IntentClassificationResponse>(generatedResponse);

                if (intentResponse == null || !Enum.TryParse<QueryIntent>(intentResponse.Intent, true, out var intent))
                {
                    _logger.LogWarning("Failed to parse intent from LLM response: {Response}", generatedResponse);
                    return FallbackClassification(query);
                }

                _logger.LogInformation("Classified query intent as {Intent}: {Reasoning}", intent,
                    intentResponse.Reasoning);

                return new QueryIntentResult
                {
                    Intent = intent,
                    Reasoning = intentResponse.Reasoning ?? "No reasoning provided"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying query intent, using fallback");
                return FallbackClassification(query);
            }
        }

        /// <summary>
        /// Fallback pattern-based classification when LLM fails
        /// </summary>
        private QueryIntentResult FallbackClassification(string query)
        {
            var lowerQuery = query.ToLowerInvariant();

            // Check for exhaustive indicators - these keywords indicate the user wants complete coverage
            var exhaustiveKeywords = new[]
            {
                "list all", "find all", "show all", "every", "all cases",
                "all instances", "all documents", "all mentions", "complete list", "exhaustive", "entire",
                "give me every", "what are all", "all of", "each"
            };

            if (exhaustiveKeywords.Any(keyword => lowerQuery.Contains(keyword)))
            {
                return new QueryIntentResult
                {
                    Intent = QueryIntent.Exhaustive,
                    Reasoning = "Pattern-based fallback: Contains exhaustive keywords"
                };
            }

            // Default to regular for all other queries
            return new QueryIntentResult
            {
                Intent = QueryIntent.Regular,
                Reasoning = "Pattern-based fallback: Default to regular intent"
            };
        }

        // Internal classes for JSON deserialization
        private class ChatCompletionResponse
        {
            [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")] public MessageContent? Message { get; set; }
        }

        private class MessageContent
        {
            [JsonPropertyName("content")] public string? Content { get; set; }
        }

        private class IntentClassificationResponse
        {
            [JsonPropertyName("intent")] public string Intent { get; set; }

            [JsonPropertyName("reasoning")] public string Reasoning { get; set; }
        }
    }
}