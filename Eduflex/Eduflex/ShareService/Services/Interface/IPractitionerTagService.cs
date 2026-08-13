using ShareService.Models.VisaProcess;

namespace ShareService.Services.Interface
{
    public interface IPractitionerTagService
    {
        // Lightweight directory, no permission gate — the Process Designer's step editor
        // needs this list for any authenticated staff viewing a template, same reasoning as
        // IVisaProcessTemplateService.GetAllAsync.
        Task<List<PractitionerTagModel>> GetAllAsync();

        Task<PractitionerTagModel> CreateAsync(PractitionerTagModel tag, string userId);
        Task<bool> UpdateAsync(string id, PractitionerTagModel tag, string userId);
        Task<bool> SetActiveAsync(string id, bool isActive, string userId);
    }
}
