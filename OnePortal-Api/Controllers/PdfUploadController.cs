using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfUploadController(IWatermarkService watermarkService) : ControllerBase
    {
        private readonly IWatermarkService _watermarkService = watermarkService;

        [HttpPost("upload-pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected for upload.");
            }

            string folderName = "uploads";
            var watermarkedFilePath = await _watermarkService.AddWatermarkToPdfAspose(file, "Confidential", folderName);

            return Ok(new
            {
                WatermarkedFile = watermarkedFilePath.Replace("wwwroot/", "")
            });
        }
    }
}