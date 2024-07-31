using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnePortal_Api.Model
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tax_Id { get; set; }
        public string address_sup { get; set; }
        public string district { get; set; }
        public string subdistrict { get; set; }
        public string province { get; set; }
        public string postalCode { get; set; }
        public string tel { get; set; }
        public string email { get; set; }
        public string customer_num { get; set; }
        public string customer_type { get; set;}
        public string site { get; set;}
        public string status { get; set; }
        public int user_id { get; set; }
        public string company { get; set; }
    }
}
