using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class BankMasterData
    {
        [Key]
        public int BankId { get; set; }
        public required string BankName { get; set; }
        public required string BankNumber { get; set; }
        public required string AlternateBankName { get; set; }
        public required string ShortBankName { get; set; }
    }
}
