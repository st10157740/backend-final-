using Azure.Storage.Blobs;

namespace MbabaneHighlandersBackend2.Services
{
    public class BlobUpload : IBlobUpload
    {
        private readonly IConfiguration _config;

        public BlobUpload(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            var connectionString = _config["Azure:StorageConnectionString"];
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }
    }
}
