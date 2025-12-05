namespace rag_experiment.Services.FilingDownloader;

public interface IFilingDownloadClient
{
    public Task<string> DownloadFilingAsync(List<string> filingsToDownload, string companyName, string conversationId);
}