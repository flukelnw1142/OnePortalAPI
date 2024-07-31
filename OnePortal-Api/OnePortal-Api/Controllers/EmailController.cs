using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OnePortal_Api.Services;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [EnableCors("AllowSpecificOrigin")]
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
    {
        if (ModelState.IsValid)
        {
            await _emailService.SendEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body);
            return Ok(new { Message = "Email sent successfully" });
        }
        return BadRequest(ModelState);
    }
}

public class EmailRequest
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
