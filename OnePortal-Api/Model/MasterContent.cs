using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class MasterContent
    {
        [Key]
        public int ID { get; set; }
        public required string Title { get; set; }
        public string? Content { get; set; }
    }
}
