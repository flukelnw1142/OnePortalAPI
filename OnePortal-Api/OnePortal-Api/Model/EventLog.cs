using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class EventLog
    {
        [Key]
        public int id { get; set; }
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string status { get; set; }
        public int? customer_id { get; set; } // อนุญาตให้เป็นค่า NULL
        public int? supplier_id { get; set; } // อนุญาตให้เป็นค่า NULL
        public DateTime time { get; set; }
        public string reject_reason { get; set; } // อนุญาตให้เป็นค่า NULL
    }
}
