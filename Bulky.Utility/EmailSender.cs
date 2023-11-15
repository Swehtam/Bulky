using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Bulky.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly string UsernameAuth;
        private readonly string PasswordAuth;

        public EmailSender(IConfiguration config)
        {
            UsernameAuth = config.GetSection("GoogleApps:GoogleAccount").Get<string>();
            PasswordAuth = config.GetSection("GoogleApps:SMTPPassword").Get<string>();
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailToSend = new MimeMessage();
            emailToSend.From.Add(MailboxAddress.Parse(UsernameAuth));
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.Subject = subject;
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = htmlMessage
            };

            using(var emailClient = new SmtpClient())
            {
                emailClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                emailClient.Authenticate(UsernameAuth, PasswordAuth);
                emailClient.Send(emailToSend);
                emailClient.Disconnect(true);
            }

            return Task.CompletedTask;
        }
    }
}
