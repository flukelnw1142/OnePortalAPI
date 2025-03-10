using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Dto
{
    public class SupplierBankDto
    {
        [Key]
        public int SupbankId { get; set; }
        public int SupplierId { get; set; }

        public required string NameBank { get; set; }
        public required string Branch { get; set; }
        public required string AccountNum { get; set; }
        public required string AccountName { get; set; }
        public required string SupplierGroup { get; set; }
        public required string Company { get; set; }
        public required Dictionary<string, List<string>> LabelTextsV2 { get; set; }
    }
}