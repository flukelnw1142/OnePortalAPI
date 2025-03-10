using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class PDPAConsent
    {
        [Key]
        public int Id { get; set; }
        public required string Username { get; set; }
        public Boolean Accepted { get; set; }
        public DateTime Time { get; set; }
    }
}
