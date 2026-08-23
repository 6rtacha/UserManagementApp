using System.Net;
using System.Net.Mail;

namespace UserManagementApp.Services;

public class GmailSmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailSmtpEmailService> _logger;

    public GmailSmtpEmailService(IConfiguration configuration, ILogger<GmailSmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationLink)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var senderEmail = _configuration["Smtp:SenderEmail"];
            var senderPassword = _configuration["Smtp:SenderPassword"]?.Replace(" ", ""); // Remove spaces from 16-character app password
            var senderName = _configuration["Smtp:SenderName"] ?? "The App - User Management";

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogWarning("Gmail SMTP credentials (SenderEmail or SenderPassword) are not configured.");
                return false;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = "Verify your email address - The App",
                Body = $@"
                    <div style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                        <div style='text-align: center; margin-bottom: 24px;'>
                            <h2 style='color: #2563eb; margin: 0; letter-spacing: 0.1em;'>THE APP</h2>
                        </div>
                        <h3 style='color: #1e293b; margin-top: 0;'>Welcome, {userName}!</h3>
                        <p style='color: #475569; font-size: 15px; line-height: 1.6;'>
                            Thank you for creating an account. Please click the button below to verify your email address and activate your account:
                        </p>
                        <div style='text-align: center; margin: 32px 0;'>
                            <a href='{verificationLink}' style='background-color: #2563eb; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; display: inline-block; font-size: 15px;'>
                                Verify Email Address
                            </a>
                        </div>
                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;' />
                        <p style='color: #94a3b8; font-size: 13px; margin-bottom: 8px;'>If the button above does not work, copy and paste this link into your browser:</p>
                        <p style='color: #2563eb; font-size: 12px; word-break: break-all;'>{verificationLink}</p>
                    </div>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification email successfully sent to {Email} via Gmail SMTP", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email} via Gmail SMTP", toEmail);
            return false;
        }
    }
}
