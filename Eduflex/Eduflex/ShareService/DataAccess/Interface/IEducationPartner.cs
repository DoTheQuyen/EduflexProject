using ShareService.Common;
using ShareService.Models.EducationPartner;

namespace ShareService.DataAccess.Interface
{
    public interface IEducationPartner
    {
        Task<bool> CreateEducationPartnerAsync(EducationPartnerModel partner);
        Task<EducationPartnerModel?> GetEducationPartnerByIdAsync(string id);
        Task<List<EducationPartnerModel>> GetByIdsAsync(IEnumerable<string> ids);
        Task<PagedResult<EducationPartnerModel>> GetEducationPartnersAsync(EducationPartnerFilter filter);
        Task<bool> UpdateEducationPartnerAsync(string id, EducationPartnerModel partner);
        Task<bool> DeleteEducationPartnerAsync(string id);
        Task<bool> ExistsWithBusinessPartnerIdAsync(string businessPartnerId);
    }
}
