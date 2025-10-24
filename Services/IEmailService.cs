using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Barangay.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task<bool> SendOTPEmailAsync(string email, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email configuration is missing. Please check appsettings.json");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Care"),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {email}");
            }
        }

        public async Task<bool> SendOTPEmailAsync(string email, string otp)
        {
            try
            {
                // Get email configuration from appsettings.json
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email configuration is missing. Please check appsettings.json");
                    return false;
                }

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Care"),
                    Subject = "Your OTP Code - Baesa Health Care",
                    Body = GenerateOTPEmailBody(otp),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                
                _logger.LogInformation($"OTP email sent successfully to {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP email to {email}");
                return false;
            }
        }

        private string GenerateOTPEmailBody(string otp)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
                        .otp-code {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-radius: 5px; letter-spacing: 5px; }}
                        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 5px 5px; font-size: 14px; color: #6c757d; }}
                        .warning {{ color: #dc3545; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>🔐 Baesa Health Care</h2>
                            <p>One-Time Password (OTP) Verification</p>
                        </div>
                        
                        <div class='content'>
                            <h3>Hello!</h3>
                            <p>You have requested to log in to your Baesa Health Care account. Please use the following One-Time Password (OTP) to complete your login:</p>
                            
                            <div class='otp-code'>{otp}</div>
                            
                            <p><strong>Important Security Information:</strong></p>
                            <ul>
                                <li>This OTP is valid for <span class='warning'>5 minutes</span> only</li>
                                <li>Do not share this code with anyone</li>
                                <li>If you did not request this login, please ignore this email</li>
                            </ul>
                            
                            <p>If you have any concerns about this login attempt, please contact our support team immediately.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from Baesa Health Care System</p>
                            <p>Please do not reply to this email</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
} 