using ShareService.Models.VisaProcess;

namespace ShareService.Services.Interface
{
    public interface IVisaProcessTemplateService
    {
        // Lightweight directory, no permission gate — any authenticated staff member may
        // need this list (e.g. to see what process a business runs for a given
        // country/category), same reasoning as IDynamicFormTemplateService.GetAllAsync.
        Task<List<VisaProcessTemplateModel>> GetAllAsync();

        Task<VisaProcessTemplateModel?> GetByIdAsync(string id, string userId);
        Task<VisaProcessTemplateModel> CreateAsync(VisaProcessTemplateModel template, string userId);
        Task<bool> UpdateAsync(string id, VisaProcessTemplateModel template, string userId);
        Task<bool> SetStatusAsync(string id, bool isActive, string userId);
    }
}
