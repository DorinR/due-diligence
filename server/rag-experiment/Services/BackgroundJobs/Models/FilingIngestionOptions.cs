namespace rag_experiment.Services.BackgroundJobs.Models
{
    /// <summary>
    /// Configuration settings for filing ingestion defaults.
    /// </summary>
    public class FilingIngestionOptions
    {
        /// <summary>
        /// Filing types to download by default (e.g., 10-K, 10-Q).
        /// </summary>
        public List<string> DefaultFilingTypes { get; set; } = new();
    }
}

