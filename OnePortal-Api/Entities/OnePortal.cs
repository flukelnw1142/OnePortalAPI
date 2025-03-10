namespace OnePortal_Api.Entities
{
    public class OnePortal

    {
        public int UserId  { get; set; }
        public required string Firstname { get; set; }
        public required string Lastname { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required string Status { get; set; }

    }
}
