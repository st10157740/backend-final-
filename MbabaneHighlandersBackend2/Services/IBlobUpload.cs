namespace MbabaneHighlandersBackend2.Services
{
    public interface IBlobUpload
    {
        Task<string> UploadFileAsync(IFormFile file, string containerName);
    }
}
