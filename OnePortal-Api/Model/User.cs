using System.ComponentModel.DataAnnotations;

namespace OnePortal_Api.Model
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public required string Firstname { get; set; }
        public required string Lastname { get; set; }
        public required string Email { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Company { get; set; }
        public string? EmpNo { get; set; }
        public int ResponseType { get; set; }
        public string? tel { get; set; }
    }
}
