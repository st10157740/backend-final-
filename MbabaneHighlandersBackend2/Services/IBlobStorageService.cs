namespace MbabaneHighlandersBackend2.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadProofAsync(IFormFile file, string memberName);
    }
}
