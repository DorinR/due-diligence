using System.Threading.Tasks;

namespace rag_experiment.Services.BackgroundJobs
{
    public interface IDocumentProcessingJobService
    {
        /// <summary>
        /// Sets up the document processing pipeline by initializing state and enqueueing
        /// the chain of background jobs. Call this synchronously - only the actual 
        /// processing steps run as background jobs.
        /// </summary>
        /// <returns>The job ID of the final job in the chain, for tracking purposes</returns>
        Task<string> SetupProcessingPipeline(int documentId, string filePath, int userId, int conversationId);
    }
}