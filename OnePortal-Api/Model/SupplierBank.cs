using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnePortal_Api.Model
{
    public class SupplierBank
    {
        [Key]
        public int SupbankId { get; set; }
        public int SupplierId { get; set; }
        public required string NameBank { get; set; }
        public required string Branch { get; set; }
        public required string AccountNum { get; set; }
        public required string AccountName { get; set; }
        public required string SupplierGroup { get; set; }
        public required string Company { get; set; }
    }
}
