namespace OnePortal_Api.Dto
{
    public class EventLogDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Status { get; set; }
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public DateTime Time { get; set; }
        public string? RejectReason { get; set; }
        public string? FullName { get; set; }
        public string? Tel { get; set; }
        public int RoleId { get; set; }
        public string? Payment { get; set; }
    }

}
