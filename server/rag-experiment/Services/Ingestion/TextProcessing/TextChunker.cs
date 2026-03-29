using Microsoft.Extensions.Options;
using rag_experiment.Services.Query.Models;

namespace rag_experiment.Services
{
    public class TextChunker : ITextChunker
    {
        private readonly RagSettings _ragSettings;

        /// <summary>
        /// Initializes a new instance of TextChunker with configuration settings
        /// </summary>
        /// <param name="ragSettings">RAG configuration settings containing chunk size and overlap</param>
        public TextChunker(IOptions<RagSettings> ragSettings)
        {
            _ragSettings = ragSettings.Value;
        }

        public List<string> ChunkText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var maxChunkSize = _ragSettings.Chunking.ChunkSize;
            var overlap = _ragSettings.Chunking.ChunkOverlap;

            var chunks = new List<string>();
            var sentences = SplitIntoSentences(text);
            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var sentence in sentences)
            {
                var sentenceParts = SplitOversizedSegment(sentence, maxChunkSize);

                foreach (var sentencePart in sentenceParts)
                {
                // If adding this sentence would exceed maxChunkSize
                    if (currentLength + sentencePart.Length > maxChunkSize && currentChunk.Any())
                    {
                        // Add the current chunk to our list of chunks
                        chunks.Add(string.Join(" ", currentChunk));

                        // Start a new chunk with overlap
                        currentChunk = GetOverlappingContent(currentChunk, overlap);
                        currentLength = currentChunk.Sum(s => s.Length + 1); // +1 for space
                    }

                    currentChunk.Add(sentencePart);
                    currentLength += sentencePart.Length + 1; // +1 for space
                }
            }

            // Add the last chunk if there's anything left
            if (currentChunk.Any())
            {
                var chunk = string.Join(" ", currentChunk);
                chunks.Add(chunk);
            }

            return chunks;
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Simple sentence splitting - can be made more sophisticated
            return text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private List<string> SplitOversizedSegment(string text, int maxChunkSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            if (text.Length <= maxChunkSize)
                return new List<string> { text.Trim() };

            var parts = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // If there are no meaningful word boundaries, fall back to hard character splits.
            if (words.Length <= 1)
            {
                for (var i = 0; i < text.Length; i += maxChunkSize)
                {
                    var length = Math.Min(maxChunkSize, text.Length - i);
                    parts.Add(text.Substring(i, length).Trim());
                }

                return parts.Where(part => !string.IsNullOrWhiteSpace(part)).ToList();
            }

            var currentPart = new List<string>();
            var currentLength = 0;

            foreach (var word in words)
            {
                if (word.Length > maxChunkSize)
                {
                    if (currentPart.Count > 0)
                    {
                        parts.Add(string.Join(" ", currentPart));
                        currentPart.Clear();
                        currentLength = 0;
                    }

                    for (var i = 0; i < word.Length; i += maxChunkSize)
                    {
                        var length = Math.Min(maxChunkSize, word.Length - i);
                        parts.Add(word.Substring(i, length));
                    }

                    continue;
                }

                if (currentLength + word.Length + (currentPart.Count > 0 ? 1 : 0) > maxChunkSize)
                {
                    parts.Add(string.Join(" ", currentPart));
                    currentPart.Clear();
                    currentLength = 0;
                }

                currentPart.Add(word);
                currentLength += word.Length + (currentPart.Count > 1 ? 1 : 0);
            }

            if (currentPart.Count > 0)
            {
                parts.Add(string.Join(" ", currentPart));
            }

            return parts.Where(part => !string.IsNullOrWhiteSpace(part)).ToList();
        }

        private List<string> GetOverlappingContent(List<string> currentChunk, int overlapSize)
        {
            var overlappingContent = new List<string>();
            var currentSize = 0;

            // Work backwards through the current chunk
            for (int i = currentChunk.Count - 1; i >= 0; i--)
            {
                var sentence = currentChunk[i];
                if (currentSize + sentence.Length > overlapSize)
                    break;

                overlappingContent.Insert(0, sentence);
                currentSize += sentence.Length + 1; // +1 for space
            }

            return overlappingContent;
        }
    }
}
