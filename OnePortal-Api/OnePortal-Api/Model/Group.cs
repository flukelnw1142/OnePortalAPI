using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Group
    {
        [Key]
        public int id { get; set; }
        public string group_name { get; set; }
        public  ICollection<GroupDetail> groupDetails { get; set; }
    }

    public class GroupDetail
    {
        [Key]
        public int id { get; set; }
        public string group_detail_name { get; set; }
        public int group_id { get; set; }
        public Group Group { get; set; }
    }
}
