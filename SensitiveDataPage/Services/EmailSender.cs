using System.Net;
using System.Net.Mail;

namespace SensitiveDataPage.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration configuration;

        public EmailSender(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var mail = configuration["EmailStrings:EmailAddress"];
            var password = configuration["EmailStrings:EmailPassword"];

            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, password)
            };

            return smtpClient.SendMailAsync(new MailMessage(from: mail, to: toEmail, subject, message)); 
        }
    }
}
