using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Models.Setting;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services.Service.Integration
{
    public class AzureEmailService : IAzureEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly string _senderAddress;
        private readonly ILogger<AzureEmailService> _logger;

        public AzureEmailService(IOptions<AzureEmailSettings> settings, ILogger<AzureEmailService> logger)
        {
            var config = settings.Value;
            _emailClient = new EmailClient(config.ConnectionString);
            _senderAddress = config.SenderAddress;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string htmlBody, string? plainTextBody = null)
        {
            try
            {
                var emailContent = new EmailContent(subject)
                {
                    Html = htmlBody,
                    PlainText = plainTextBody
                };

                var emailMessage = new EmailMessage(_senderAddress, toAddress, emailContent);

                EmailSendOperation sendOperation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

                _logger.LogInformation("Email sent to {ToAddress} with subject '{Subject}', status {Status}",
                    toAddress, subject, sendOperation.Value.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToAddress} with subject '{Subject}'", toAddress, subject);
                throw;
            }
        }
    }
}