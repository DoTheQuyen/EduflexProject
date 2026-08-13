using ShareService.Models.VisaProcess;

namespace ShareService.DataAccess.Interface
{
    public interface IVisaProcessTemplate
    {
        Task<List<VisaProcessTemplateModel>> GetAllAsync();
        Task<VisaProcessTemplateModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(VisaProcessTemplateModel template);
        Task<bool> ReplaceAsync(string id, VisaProcessTemplateModel template);
    }
}
