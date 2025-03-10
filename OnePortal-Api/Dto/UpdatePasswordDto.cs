namespace OnePortal_Api.Dto
{
    public class UpdatePasswordDto
    {
        public required string Username { get; set; }
        public required string NewPassword { get; set; }
    }
}
