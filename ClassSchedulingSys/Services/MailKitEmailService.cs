using ClassSchedulingSys.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace ClassSchedulingSys.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseStartTls { get; set; } = true;
        public string FromName { get; set; } = "ClassSchedulingSys";
        public string FromAddress { get; set; } = string.Empty;
    }

    public class MailKitEmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        public MailKitEmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            // security: prefer StartTls where possible
            var secureSocket = _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_settings.Host, _settings.Port, secureSocket, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
