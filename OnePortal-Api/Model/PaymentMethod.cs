using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }
        public required string PaymentMethodName { get; set; }
        public required string Description { get; set; }

    }
}
