namespace rag_experiment.Services.BackgroundJobs;

public interface IDocumentProcessingJobService
{
    /// <summary>
    /// Sets up the filing ingestion pipeline by enqueueing the chain of background jobs.
    /// The pipeline downloads filings for a company, extracts text, chunks it,
    /// generates embeddings, and persists them to the database.
    /// </summary>
    /// <param name="companyIdentifier">The company ticker symbol or CIK number.</param>
    /// <param name="filingTypes">List of filing types to download (e.g., ["10-K", "10-Q", "8-K"]).</param>
    /// <param name="userId">The user who initiated the ingestion.</param>
    /// <param name="conversationId">The conversation to associate the documents with.</param>
    /// <returns>The job ID of the final job in the chain, for tracking purposes.</returns>
    Task<string> SetupFilingIngestionPipeline(
        string companyIdentifier,
        List<string> filingTypes,
        int userId,
        int conversationId);
}
