using ShareService.Common;
using ShareService.Models.EducationPartner;

namespace ShareService.Services.Interface
{
    public interface IEducationPartnerService
    {
        Task<bool> CreateEducationPartner(EducationPartnerModel partner, string userId);
        Task<EducationPartnerModel?> GetEducationPartnerById(string id, string userId);

        // Deliberately unchecked — shared by the student-facing "directory" read path
        // (any logged-in user, no EducationPartnersView permission required) as well as
        // the staff search below. Do not add a permission check here.
        Task<PagedResult<EducationPartnerModel>> GetEducationPartners(EducationPartnerFilter filter);

        // The staff-gated equivalent of GetEducationPartners above — same underlying
        // query, but requires EducationPartnersView first.
        Task<PagedResult<EducationPartnerModel>> SearchEducationPartners(EducationPartnerFilter filter, string userId);

        Task<bool> UpdateEducationPartner(string id, EducationPartnerModel partner, string userId);
        Task<bool> DeleteEducationPartner(string id, string userId);
    }
}
