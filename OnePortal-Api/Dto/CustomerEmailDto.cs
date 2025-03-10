namespace OnePortal_Api.Dto
{
    public class CustomerEmailDto
    {
        public int CustomerId { get; set; }
        public required string CustomerNumber { get; set; }
        public DateTime UpdateTimestamp { get; set; }
        public required string UserEmail { get; set; }
        public required string Site { get; set; }
        public required string OwnerAccount { get; set; }
        public required string Name { get; set; }
        public required string Company { get; set; }
    }
}
