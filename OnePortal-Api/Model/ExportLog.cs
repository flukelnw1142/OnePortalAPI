using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class ExportLog
    {
        [Key]
        public int Id { get; set; }
        public required string Username { get; set; }
        public DateTime Time { get; set; }
    }
}
