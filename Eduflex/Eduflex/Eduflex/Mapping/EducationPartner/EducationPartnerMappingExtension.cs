using Eduflex.DTOs.EducationPartner;
using ShareService.Models.EducationPartner;

namespace Eduflex.Mapping.EducationPartner
{
    public static class EducationPartnerMappingExtension
    {
        public static EducationPartnerFilter ToFilter(this EducationPartnerFilterDto dto)
        {
            return new EducationPartnerFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

        public static EducationPartnerModel ToModel(this CreateEducationPartnerDto dto)
        {
            return new EducationPartnerModel
            {
                Name = dto.Name,
                Location = dto.Location,
                Trademark = dto.Trademark,
                LogoUrl = dto.LogoUrl,
                Description = dto.Description,
                Country = dto.Country,
                PartnerType = dto.PartnerType,
                Intakes = dto.Intakes,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                BusinessPartnerId = dto.BusinessPartnerId,
                CommissionBaseRate = dto.CommissionBaseRate,
                Abn = dto.Abn,
                Acn = dto.Acn
            };
        }

        // Staff-only DTO. businessPartnerName is resolved by the caller (a bulk lookup
        // against IBusinessPartnerService, the same batching pattern already used for
        // courses) — kept optional here so callers that don't need the link (e.g. a
        // partner that has none) don't have to special-case it.
        public static EducationPartnerDto ToDto(this EducationPartnerModel model, List<CourseDto> courses, string? businessPartnerName = null)
        {
            return new EducationPartnerDto
            {
                Id = model.Id,
                Name = model.Name,
                Location = model.Location,
                Trademark = model.Trademark,
                LogoUrl = model.LogoUrl,
                Description = model.Description,
                Country = model.Country,
                PartnerType = model.PartnerType,
                Intakes = model.Intakes,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                BusinessPartnerId = model.BusinessPartnerId,
                BusinessPartnerName = businessPartnerName,
                CommissionBaseRate = model.CommissionBaseRate,
                Abn = model.Abn,
                Acn = model.Acn,
                Courses = courses,
                CreatedAt = model.CreatedAt
            };
        }

        // Public-facing directory DTO — never includes BusinessPartnerId/CommissionBaseRate/Abn/Acn.
        public static EducationPartnerDirectoryDto ToDirectoryDto(this EducationPartnerModel model, List<CourseDto> courses)
        {
            return new EducationPartnerDirectoryDto
            {
                Id = model.Id,
                Name = model.Name,
                Location = model.Location,
                Trademark = model.Trademark,
                LogoUrl = model.LogoUrl,
                Description = model.Description,
                Country = model.Country,
                PartnerType = model.PartnerType,
                Intakes = model.Intakes,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Courses = courses,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
