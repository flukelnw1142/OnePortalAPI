namespace OnePortal_Api.Model
{
    public class EmailLog
    {
        public int Id { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

}
