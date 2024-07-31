using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Company
    {
        [Key]
        public int com_code { get; set; }
        public string full_name { get; set; }
        public string abbreviation { get; set; }
        public string group_name { get; set; }
    }
}
