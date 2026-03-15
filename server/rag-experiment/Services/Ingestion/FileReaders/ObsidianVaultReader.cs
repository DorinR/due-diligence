namespace rag_experiment.Services
{
    public class ObsidianVaultReader : IObsidianVaultReader
    {
        private readonly ILogger<ObsidianVaultReader> _logger;

        public ObsidianVaultReader(ILogger<ObsidianVaultReader> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> ReadMarkdownFilesAsync(string vaultPath)
        {
            if (!Directory.Exists(vaultPath))
            {
                throw new DirectoryNotFoundException($"Obsidian vault directory not found at: {vaultPath}");
            }

            var markdownFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);
            var result = new Dictionary<string, string>();

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    result[filePath] = content;
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Error reading markdown file {FilePath}", filePath);
                }
            }

            return result;
        }
    }
}
