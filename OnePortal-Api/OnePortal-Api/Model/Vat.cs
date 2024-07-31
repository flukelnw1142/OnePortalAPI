using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Vat
    {
        [Key]
        public int Id { get; set; }
        public string InputTaxCode { get; set; }
        public string TaxDescription { get; set; }
        public int TaxRate { get; set; }
        public string TaxAccount { get; set; }
        public string InterimTaxAccount { get; set; }
    }
}
