using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class UserResponsible
    {
        [Key]
        public int id { get; set; }
        public required string ResponseType { get; set; }
    }
}
