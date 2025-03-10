using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class CustomerTypeMasterData
    {
        [Key]
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string CodeFrom { get; set; }
        public required string Meaning { get; set; }
        public required string Description { get; set; }
    }
}
