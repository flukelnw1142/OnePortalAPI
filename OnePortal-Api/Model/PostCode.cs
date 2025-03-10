using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class PostCode
    {
        [Key]
        public int PostId { get; set; }
        public required string PostalCode { get; set; }
        public required string Subdistrict { get; set; }
        public required string District { get; set; }
        public required string Province { get; set; }
    }
}
