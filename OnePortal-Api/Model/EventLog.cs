using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class EventLog
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Status { get; set; }
        public int? CustomerId { get; set; } // อนุญาตให้เป็นค่า NULL
        public int? SupplierId { get; set; } // อนุญาตให้เป็นค่า NULL
        public DateTime Time { get; set; }
        public string? RejectReason { get; set; } // อนุญาตให้เป็นค่า NULL
    }
}
