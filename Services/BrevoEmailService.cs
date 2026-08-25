using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UserManagementApp.Services;

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationLink)
    {
        try
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderEmail = _configuration["Brevo:SenderEmail"];
            var senderName = _configuration["Brevo:SenderName"] ?? "The App - User Management";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("Brevo API credentials (Brevo:ApiKey or Brevo:SenderEmail) are not configured.");
                return false;
            }

            var payload = new
            {
                sender = new
                {
                    name = senderName,
                    email = senderEmail
                },
                to = new[]
                {
                    new
                    {
                        email = toEmail,
                        name = string.IsNullOrWhiteSpace(userName) ? "User" : userName
                    }
                },
                subject = "Verify your email address - The App",
                htmlContent = $@"
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
                    </div>"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API error (Status: {StatusCode}): {ErrorBody}", response.StatusCode, responseBody);
                return false;
            }

            _logger.LogInformation("Verification email successfully accepted by Brevo for {Email}. Response: {Response}", toEmail, responseBody);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email} via Brevo HTTP API", toEmail);
            return false;
        }
    }
}
