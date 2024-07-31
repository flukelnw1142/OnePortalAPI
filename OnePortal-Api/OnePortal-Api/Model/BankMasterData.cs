using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class BankMasterData
    {
        [Key]
        public int bank_id { get; set; }
        public string bank_name { get; set; }
        public string bank_number { get; set; }
        public string alternate_bank_name { get; set; }
        public string short_bank_name { get; set; }
    }
}
