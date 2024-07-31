using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Role
    {
        [Key]
        public int id { get; set; }
        public string role_name { get; set; }
        public string action { get; set;}
    }
}
