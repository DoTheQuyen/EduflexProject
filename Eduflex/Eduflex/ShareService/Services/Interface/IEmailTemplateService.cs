using ShareService.Models.Enrolment;

namespace ShareService.Services.Interface
{
    public interface IEmailTemplateService
    {
        Task<List<EmailTemplateModel>> GetAllAsync(string userId);
        Task<EmailTemplateModel> CreateAsync(EmailTemplateModel template, string userId);
        Task<bool> UpdateAsync(string id, EmailTemplateModel template, string userId);
        Task<bool> DeleteAsync(string id, string userId);
    }
}
