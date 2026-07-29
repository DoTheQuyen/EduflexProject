namespace ShareService.Services.Interface.Integration
{
    public interface IAzureBlobDocStorageService
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteAsync(string blobUrl);

        // Read-only, time-limited SAS URL for a blob that's otherwise served via its
        // permanent, unsigned UploadAsync URL — used when emailing a document link to
        // someone outside the system, so the link stops working after N days.
        Uri GetExpiringDownloadUri(string blobUrl, int expiryDays);
    }
}