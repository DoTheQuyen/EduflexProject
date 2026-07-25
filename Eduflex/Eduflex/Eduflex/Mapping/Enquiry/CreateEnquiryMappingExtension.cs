using Eduflex.DTOs.Enquiry;
using ShareService.Common;
using ShareService.Models.Enquiry;

namespace Eduflex.Mapping.Enquiry
{
    public static class CreateEnquiryMappingExtension
    {
        public static EnquiryFilter ToFilter(this EnquiryFilterDto dto)
        {
            return new EnquiryFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm,
                Status = dto.Status
            };
        }

        public static EnquiryModel ToModel(this CreateEnquiryDto dto)
        {
            return new EnquiryModel
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Email = dto.Email,
                Mobile = dto.Mobile,
                Enquiry = dto.Enquiry,
                RecaptchaToken = dto.RecaptchaToken
            };
        }

        public static EnquiryDto ToDto(this EnquiryModel model)
        {
            return new EnquiryDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Email = model.Email,
                Mobile = model.Mobile,
                Enquiry = model.Enquiry,
                Status = model.Status,
                Response = model.Response,
            };
        }

        public static EnquiryModel ToModel(this EnquiryDto model)
        {
            return new EnquiryModel
            {
                Id = model.Id,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Email = model.Email,
                Mobile = model.Mobile,
                Enquiry = model.Enquiry,
                Status = model.Status,
                Response = model.Response,
            };
        }
    }
}
