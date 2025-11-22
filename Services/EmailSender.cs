using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using Resend;

namespace SocialNetworkApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IResend _resend;
        private readonly string? _fromEmail;

        public EmailSender(IConfiguration configuration)
        {
            var apiKey = configuration["Resend:ApiKey"];
            _resend = ResendClient.Create(apiKey!);
            _fromEmail = configuration["Resend:FromEmail"];
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            var message = new EmailMessage
            {
                From = _fromEmail!,
                To = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await _resend.EmailSendAsync(message);
        }
    }
}
