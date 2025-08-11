using System.Net;
using System.Net.Mail;

namespace NewsSite1.Services
{
    /// <summary>
    /// Sends basic HTML emails using Gmail SMTP.
    /// </summary>
    public class EmailService
    {
        // SMTP settings (kept as-is). In production, move secrets to config/secret store.
        private readonly string smtpHost = "smtp.gmail.com";
        private readonly int smtpPort = 587; // TLS port
        private readonly string senderEmail = "newspapperproject@gmail.com";
        private readonly string senderPassword = "cgouasarvswsxbns";

        /// <summary>
        /// Sends an HTML email to a single recipient.
        /// Throws on failure (caller can catch).
        /// </summary>
        public void Send(string toEmail, string subject, string bodyHtml)
        {
            // Build SMTP client (network delivery, SSL/TLS)
            using var smtp = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000 // ms
            };

            // Create the email message (HTML body)
            using var mail = new MailMessage(senderEmail, toEmail, subject, bodyHtml)
            {
                IsBodyHtml = true
            };

            // Send (synchronous). Exceptions bubble up for the caller to handle.
            smtp.Send(mail);
        }
    }
}
