using Aspose.Pdf;
using Aspose.Pdf.Text;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class WatermarkServiceAspose : IWatermarkService
    {
        private readonly string _uploadsPath;

        public WatermarkServiceAspose()
        {
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<string> AddWatermarkToPdfAspose(IFormFile file, string watermarkText, string folderName)
        {
            string uploadPath = Path.Combine("wwwroot/uploads", folderName);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }
            var originalFileName = "";
            if (folderName == "") {
                originalFileName = $"{file.FileName}";
            }
            else {
                originalFileName = $"{Guid.NewGuid()}_{file.FileName}";
            }
            
            var filePath = Path.Combine(uploadPath, originalFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var outputFilePath = "";
            if (folderName == "") {
                outputFilePath = Path.Combine(uploadPath, $"{originalFileName}");
            }
            else {
                outputFilePath = Path.Combine(uploadPath, $"watermarked_{originalFileName}");
            }          

            using (Document pdfDocument = new(filePath))
            {
                foreach (var page in pdfDocument.Pages)
                {
                    double pageWidth = page.Rect.Width;
                    double pageHeight = page.Rect.Height;

                    double fontSize = Math.Min(pageWidth, pageHeight) / 5;

                    TextStamp textStamp = new(watermarkText)
                    {
                        Opacity = 0.2f,
                        Background = false,
                        RotateAngle = 45,
                        HorizontalAlignment = HorizontalAlignment.Center, // ✅ จัดกึ่งกลางแนวนอน
                        VerticalAlignment = VerticalAlignment.Center,     // ✅ จัดกึ่งกลางแนวตั้ง
                    };

                    textStamp.TextState.Font = FontRepository.FindFont("Arial");
                    textStamp.TextState.FontSize = (float)fontSize;
                    textStamp.TextState.FontStyle = FontStyles.Bold;
                    textStamp.TextState.ForegroundColor = Aspose.Pdf.Color.FromRgb(180 / 255.0, 180 / 255.0, 180 / 255.0);

                    page.AddStamp(textStamp);
                }

                pdfDocument.Save(outputFilePath);
            }

            return Path.Combine("uploads", folderName, Path.GetFileName(outputFilePath));
        }
    }
}