using System.Net;
using System.Net.Mail;

namespace NewsSite1.Services
{
    public class EmailService
    {
        private readonly string smtpHost = "smtp.gmail.com";
        private readonly int smtpPort = 587;
        private readonly string senderEmail = "newspapperproject@gmail.com";
        private readonly string senderPassword = "cgouasarvswsxbns";

        // ✅ Sends an HTML email to a specified recipient
        public void Send(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            var mail = new MailMessage(senderEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            smtpClient.Send(mail);
        }
    }
}
