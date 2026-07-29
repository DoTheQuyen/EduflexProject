using Eduflex.DTOs.BusinessPartner;
using ShareService.Models.BusinessPartner;

namespace Eduflex.Mapping.BusinessPartner
{
    public static class BusinessPartnerMappingExtension
    {
        public static BusinessPartnerFilter ToFilter(this BusinessPartnerFilterDto dto)
        {
            return new BusinessPartnerFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

        public static BusinessPartnerContactModel ToModel(this BusinessPartnerContactDto dto)
        {
            return new BusinessPartnerContactModel
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                ContactNo = dto.ContactNo
            };
        }

        public static BusinessPartnerContactDto ToDto(this BusinessPartnerContactModel model)
        {
            return new BusinessPartnerContactDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                ContactNo = model.ContactNo
            };
        }

        public static BusinessPartnerModel ToModel(this CreateBusinessPartnerDto dto)
        {
            return new BusinessPartnerModel
            {
                Name = dto.Name,
                Trademark = dto.Trademark,
                Address = dto.Address,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Abn = dto.Abn,
                Acn = dto.Acn,
                CommissionBaseRate = dto.CommissionBaseRate,
                ContractStartDate = dto.ContractStartDate,
                ContractEndDate = dto.ContractEndDate,
                ContractFileUrl = dto.ContractFileUrl,
                ContractFileName = dto.ContractFileName,
                Contacts = dto.Contacts.Select(c => c.ToModel()).ToList()
            };
        }

        public static BusinessPartnerDto ToDto(this BusinessPartnerModel model)
        {
            return new BusinessPartnerDto
            {
                Id = model.Id,
                Name = model.Name,
                Trademark = model.Trademark,
                Address = model.Address,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Abn = model.Abn,
                Acn = model.Acn,
                CommissionBaseRate = model.CommissionBaseRate,
                ContractStartDate = model.ContractStartDate,
                ContractEndDate = model.ContractEndDate,
                ContractFileUrl = model.ContractFileUrl,
                ContractFileName = model.ContractFileName,
                Contacts = model.Contacts.Select(c => c.ToDto()).ToList(),
                CreatedAt = model.CreatedAt
            };
        }
    }
}
