using ShareService.Common;
using ShareService.Models.BusinessPartner;

namespace ShareService.DataAccess.Interface
{
    public interface IBusinessPartner
    {
        Task<bool> CreateBusinessPartnerAsync(BusinessPartnerModel partner);
        Task<BusinessPartnerModel?> GetBusinessPartnerByIdAsync(string id);
        Task<List<BusinessPartnerModel>> GetByIdsAsync(IEnumerable<string> ids);
        Task<PagedResult<BusinessPartnerModel>> GetBusinessPartnersAsync(BusinessPartnerFilter filter);
        Task<bool> UpdateBusinessPartnerAsync(string id, BusinessPartnerModel partner);
        Task<bool> DeleteBusinessPartnerAsync(string id);
    }
}
