using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using OnePortal_Api.Data;
using OnePortal_Api.Model;

namespace OnePortal_Api.Services
{
    public class EmailService(IConfiguration configuration, AppDbContext appDbContext) : IEmailService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly AppDbContext _dbContext = appDbContext;
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>() ?? throw new InvalidOperationException("SMTP settings are not configured properly.");
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(smtpSettings.SenderName, smtpSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", to));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = isHtml ? body : null,
                TextBody = !isHtml ? body : null
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();
            var emailLog = new EmailLog
            {
                RecipientEmail = to,
                Subject = subject,
                Body = body,
                Status = "Pending"
            };

            _dbContext.EmailLogs.Add(emailLog);
            await _dbContext.SaveChangesAsync();
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpSettings.Server, smtpSettings.Port, smtpSettings.UseSSL);
                if (!string.IsNullOrEmpty(smtpSettings.Username) && !string.IsNullOrEmpty(smtpSettings.Password))
                {
                    await client.AuthenticateAsync(smtpSettings.Username, smtpSettings.Password);
                }
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
                emailLog.Status = "Success"; 
            }
            catch (Exception ex)
            {
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    public class SmtpSettings
    {
        public required string Server { get; set; }
        public int Port { get; set; }
        public required string SenderName { get; set; }
        public required string SenderEmail { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public bool UseSSL { get; set; }
    }
}