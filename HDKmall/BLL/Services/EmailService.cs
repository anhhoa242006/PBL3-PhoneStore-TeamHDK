using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace HDKmall.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For testing: If configuration is missing or default, just log to console
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer) || 
                _emailSettings.SmtpServer == "smtp.gmail.com" && _emailSettings.SenderPassword == "YOUR_APP_PASSWORD")
            {
                Console.WriteLine("================ EMAIL SIMULATION ================");
                Console.WriteLine($"To: {email}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Message:\n{htmlMessage}");
                Console.WriteLine("==================================================");
                return;
            }

            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail!, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP ERROR] Failed to send email to {email}: {ex.Message}");
                Console.WriteLine("================ EMAIL SIMULATION (FALLBACK) ================");
                Console.WriteLine($"To: {email}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Message:\n{htmlMessage}");
                Console.WriteLine("=============================================================");
                throw;
            }
        }
    }
}
