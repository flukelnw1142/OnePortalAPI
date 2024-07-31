using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class SupplierTypeMasterData
    {
        [Key]
        public int id { get; set; }
        public string code { get; set; }
        public string code_from { get; set; }
        public string meaning { get; set; }
        public string description { get; set; }
    }
}
