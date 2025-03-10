using System.ComponentModel.DataAnnotations.Schema;

namespace OnePortal_Api.Dto
{
    public class SupplierBankWithFilesDto
    {
        public int? SupbankId { get; set; }
        public int? SupplierId { get; set; }
        public required string NameBank { get; set; }
        public required string Branch { get; set; }
        public required string AccountNum { get; set; }
        public required string AccountName { get; set; }
        public required string SupplierGroup { get; set; }
        public required string Company { get; set; }
        public List<IFormFile> Files { get; set; } = [];
        public required string LabelTextsJson { get; set; }
        public Dictionary<string, List<string>> LabelTextsV2 { get; set; } = [];
        public int UploadedBy { get; set; }
        [NotMapped]
        public List<string> LabelTexts { get; set; } = [];
    }
}