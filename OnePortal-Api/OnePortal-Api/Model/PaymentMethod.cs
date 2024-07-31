using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }
        public string PaymentMethodName { get; set; }
        public string Description { get; set; }

    }
}
