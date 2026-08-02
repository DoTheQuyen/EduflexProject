using ShareService.Models.Enrolment;

namespace ShareService.Services.Interface
{
    public interface IEmailTemplateService
    {
        Task<List<EmailTemplateModel>> GetAllAsync();
        Task<EmailTemplateModel?> GetByIdAsync(string id, string userId);
        Task<EmailTemplateModel> CreateAsync(EmailTemplateModel template, string userId);
        Task<bool> UpdateAsync(string id, EmailTemplateModel template, string userId);
        Task<bool> SetStatusAsync(string id, bool isActive, string userId);
    }
}
