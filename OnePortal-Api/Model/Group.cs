using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        public required string GroupName { get; set; }
        public required ICollection<GroupDetail> GroupDetails { get; set; }
    }

    public class GroupDetail
    {
        [Key]
        public int Id { get; set; }
        public required string GroupDetailName { get; set; }
        public int GroupId { get; set; }
        public required Group Group { get; set; }
    }
}
