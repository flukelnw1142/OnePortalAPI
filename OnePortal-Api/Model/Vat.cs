using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Vat
    {
        [Key]
        public int Id { get; set; }
        public required string InputTaxCode { get; set; }
        public required string TaxDescription { get; set; }
        public int TaxRate { get; set; }
        public required string TaxAccount { get; set; }
        public required string InterimTaxAccount { get; set; }
    }
}
