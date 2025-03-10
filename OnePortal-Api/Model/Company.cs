using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Company
    {
        [Key]
        public int ComCode { get; set; }
        public required string FullName { get; set; }
        public required string Abbreviation { get; set; }
        public required string GroupName { get; set; }
    }
}
