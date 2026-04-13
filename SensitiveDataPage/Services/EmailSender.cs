using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace SensitiveDataPage.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var mail = _configuration["EmailStrings:EmailAddress"];
            var password = _configuration["EmailStrings:EmailPassword"];

            if (string.IsNullOrWhiteSpace(mail))
                throw new InvalidOperationException("EmailStrings:EmailAddress is not configured.");

            var plainText = string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                plainText = Regex.Replace(message, "<.*?>", string.Empty);
            }

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(mail);
            mailMessage.To.Add(new MailAddress(toEmail));
            mailMessage.Subject = subject ?? string.Empty;
            mailMessage.IsBodyHtml = true;

            mailMessage.Body = message ?? string.Empty;

            var plainView = AlternateView.CreateAlternateViewFromString(plainText, Encoding.UTF8, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(message ?? string.Empty, Encoding.UTF8, "text/html");

            mailMessage.AlternateViews.Add(plainView);
            mailMessage.AlternateViews.Add(htmlView);

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, password)
            };

            await smtpClient.SendMailAsync(mailMessage).ConfigureAwait(false);
        }
    }
}
