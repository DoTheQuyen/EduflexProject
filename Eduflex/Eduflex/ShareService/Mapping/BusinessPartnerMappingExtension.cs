using ShareService.Models.BusinessPartner;

namespace ShareService.Mapping
{
    public static class BusinessPartnerMappingExtension
    {
        public static void ApplyEditableFields(this BusinessPartnerModel existing, BusinessPartnerModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Trademark = updateModel.Trademark;
            existing.Address = updateModel.Address;
            existing.Email = updateModel.Email;
            existing.PhoneNumber = updateModel.PhoneNumber;
            existing.Abn = updateModel.Abn;
            existing.Acn = updateModel.Acn;
            existing.CommissionBaseRate = updateModel.CommissionBaseRate;
            existing.ContractStartDate = updateModel.ContractStartDate;
            existing.ContractEndDate = updateModel.ContractEndDate;
            existing.ContractFileUrl = updateModel.ContractFileUrl;
            existing.ContractFileName = updateModel.ContractFileName;
            existing.Contacts = updateModel.Contacts;
        }
    }
}
