using ShareService.Models.Enrolment;

namespace ShareService.DataAccess.Interface
{
    public interface IEmailTemplate
    {
        Task<List<EmailTemplateModel>> GetAllAsync();
        Task<EmailTemplateModel?> GetByIdAsync(string id);
        Task<EmailTemplateModel?> GetByKeyAsync(string key);
        Task<bool> CreateAsync(EmailTemplateModel template);
        Task<bool> ReplaceAsync(string id, EmailTemplateModel template);
    }
}
