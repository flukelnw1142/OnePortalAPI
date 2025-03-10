using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        public required string RoleName { get; set; }
        public required string Action { get; set;}
    }
}
