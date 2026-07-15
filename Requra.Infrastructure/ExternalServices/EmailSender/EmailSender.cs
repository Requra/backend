using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Requra.Infrastructure.ExternalDTOs.Email;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Requra.Infrastructure.ExternalInterfaces.IEmailSender;

namespace Requra.Infrastructure.ExternalServices.EmailSender
{
    public class EmailSender(IOptions<EmailSettings> options, ILogger<EmailSender> logger) : IEmailSender
    {
        private readonly EmailSettings _settings = options.Value;

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
