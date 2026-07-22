namespace ShareService.Services.Interface.Integration
{
    public interface IAzureEmailService
    {
        Task SendEmailAsync(string toAddress, string subject, string htmlBody, string? plainTextBody = null);
    }
}