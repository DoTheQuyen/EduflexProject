namespace ShareService.Services.Interface.Integration
{
    public interface IAzureBlobDocStorageService
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteAsync(string blobUrl);
    }
}