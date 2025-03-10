using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Services;

namespace OnePortal_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController(IEmailService emailService) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;

        [EnableCors("AllowSpecificOrigin")]
        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
        {
            if (ModelState.IsValid)
            {
                //await _emailService.SendEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body);
                //return Ok(new { Message = "Email sent successfully" });
            }
            return BadRequest(ModelState);
        }
    }

    public class EmailRequest
    {
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }
    }
}