using ShareService.Models.EducationPartner;

namespace ShareService.Mapping
{
    public static class EducationPartnerMappingExtension
    {
        public static void ApplyEditableFields(this EducationPartnerModel existing, EducationPartnerModel updateModel)
        {
            existing.Name = updateModel.Name;
            existing.Location = updateModel.Location;
            existing.Trademark = updateModel.Trademark;
            existing.LogoUrl = updateModel.LogoUrl;
            existing.Description = updateModel.Description;
            existing.Country = updateModel.Country;
            existing.PartnerType = updateModel.PartnerType;
            existing.Intakes = updateModel.Intakes;
            existing.Email = updateModel.Email;
            existing.PhoneNumber = updateModel.PhoneNumber;
            existing.BusinessPartnerId = updateModel.BusinessPartnerId;
            existing.CommissionBaseRate = updateModel.CommissionBaseRate;
            existing.Abn = updateModel.Abn;
            existing.Acn = updateModel.Acn;
        }
    }
}
