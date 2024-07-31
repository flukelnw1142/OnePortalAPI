using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using OnePortal_Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>();

        if (smtpSettings == null)
        {
            throw new InvalidOperationException("SMTP settings are not configured properly.");
        }

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(smtpSettings.SenderName, smtpSettings.SenderEmail));
        emailMessage.To.Add(new MailboxAddress("", to));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("plain") { Text = body };

        try
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpSettings.Server, smtpSettings.Port, smtpSettings.UseSSL);
                if (!string.IsNullOrEmpty(smtpSettings.Username) && !string.IsNullOrEmpty(smtpSettings.Password))
                {
                    await client.AuthenticateAsync(smtpSettings.Username, smtpSettings.Password);
                }
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            throw;
        }
    }
}

public class SmtpSettings
{
    public string Server { get; set; }
    public int Port { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseSSL { get; set; }
}
