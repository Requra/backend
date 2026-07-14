using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalInterfaces.IEmailSender
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
