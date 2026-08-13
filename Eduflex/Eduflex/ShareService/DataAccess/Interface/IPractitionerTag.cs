using ShareService.Models.VisaProcess;

namespace ShareService.DataAccess.Interface
{
    public interface IPractitionerTag
    {
        Task<List<PractitionerTagModel>> GetAllAsync();
        Task<PractitionerTagModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(PractitionerTagModel tag);
        Task<bool> ReplaceAsync(string id, PractitionerTagModel tag);
    }
}
