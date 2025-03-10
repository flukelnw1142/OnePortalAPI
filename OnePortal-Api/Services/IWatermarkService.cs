namespace OnePortal_Api.Services
{
    public interface IWatermarkService
    {
        Task<string> AddWatermarkToPdfAspose(IFormFile file, string watermarkText, string folderName);
    }
}