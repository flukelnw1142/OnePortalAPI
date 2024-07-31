using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnePortal_Api.Model
{
    public class SupplierBank
    {
        [Key]
        public int supbank_id { get; set; }
        public int supplier_id { get; set; }  // Ensure this is int as per your requirements
        public string name_bank { get; set; }
        public string branch { get; set; }
        public string account_num { get; set; }
        public string account_name { get; set; }
        public string supplier_group { get; set; }
        public string company { get; set; }
    }
}
