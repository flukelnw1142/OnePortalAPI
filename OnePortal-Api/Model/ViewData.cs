using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class ViewData
    {
        [Key]
        public required string KEY { get; set; }
        public required string VENDORTYPE { get; set; }
        public required string COMPANYGRUOP { get; set; }
        public required string TAXID { get; set; }
        public required string SUPPLIERNUMBER { get; set; }
        public required string SUPPLIERNAME { get; set; }
    }
}
