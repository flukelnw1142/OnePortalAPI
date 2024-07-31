using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class PostCode
    {
        [Key]
        public int post_id { get; set; }
        public string postalCode { get; set; }
        public string subdistrict { get; set; }
        public string district { get; set; }
        public string province { get; set; }
    }
}
