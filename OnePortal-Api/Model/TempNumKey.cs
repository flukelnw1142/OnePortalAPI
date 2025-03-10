using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class TempNumKey
    {
        [Key]
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Num { get; set; }
    }
}
