using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using ShareService.Models.Setting;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
    public class AzureBlobDocStorageService : IAzureBlobDocStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobDocStorageService(IOptions<AzureBlobSettings> settings)
        {
            var config = settings.Value;
            var serviceClient = new BlobServiceClient(config.ConnectionString);
            _containerClient = serviceClient.GetBlobContainerClient(config.ContainerName);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            // The GUID prefix is only to keep the blob's storage path collision-free —
            // ContentDisposition is what a browser actually uses as the saved/downloaded
            // filename, so callers' friendly names (e.g. "FormName-StudentName-date.pdf")
            // show up correctly regardless of the underlying blob path.
            var blobName = $"{Guid.NewGuid()}-{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = contentType,
                ContentDisposition = $"inline; filename=\"{fileName}\""
            });

            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteAsync(string blobUrl)
        {
            var blobName = new Uri(blobUrl).Segments[^1];
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public Uri GetExpiringDownloadUri(string blobUrl, int expiryDays)
        {
            var blobName = new Uri(blobUrl).Segments[^1];
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Blob storage isn't configured with an account key, so expiring links can't be generated.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(expiryDays)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder);
        }
    }
}