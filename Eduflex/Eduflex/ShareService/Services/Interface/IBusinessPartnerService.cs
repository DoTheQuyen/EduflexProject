using ShareService.Common;
using ShareService.Models.BusinessPartner;

namespace ShareService.Services.Interface
{
    public interface IBusinessPartnerService
    {
        Task<bool> CreateBusinessPartner(BusinessPartnerModel partner, string userId);
        Task<BusinessPartnerModel?> GetBusinessPartnerById(string id, string userId);

        // Deliberately unchecked — shared by the Education Partner "Managed under
        // partner" link picker (any logged-in staff member browsing the dropdown, no
        // BusinessPartnersView permission required) as well as the staff search below.
        // Do not add a permission check here.
        Task<PagedResult<BusinessPartnerModel>> GetBusinessPartners(BusinessPartnerFilter filter);

        // Auth: none — deliberately open, bulk-lookup used by EducationPartnersController
        // to resolve BusinessPartnerName for the "Managed under partner" link display.
        // Same batching pattern as ICourseService.GetCoursesByPartnerIds.
        Task<List<BusinessPartnerModel>> GetBusinessPartnersByIds(IEnumerable<string> ids);

        // The staff-gated equivalent of GetBusinessPartners above — same underlying
        // query, but requires BusinessPartnersView first.
        Task<PagedResult<BusinessPartnerModel>> SearchBusinessPartners(BusinessPartnerFilter filter, string userId);

        Task<bool> UpdateBusinessPartner(string id, BusinessPartnerModel partner, string userId);
        Task<bool> DeleteBusinessPartner(string id, string userId);
    }
}
