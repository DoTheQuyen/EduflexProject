using Eduflex.API.DTOs;
using ShareService.Models.Enquiry;

namespace Eduflex.API.Mapping
{
    public static class CreateEnquiryMappingExtension
    {
        public static CreateEnquiryModel ToModel(this CreateEnquiryDto dto)
        {
            return new CreateEnquiryModel
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
                CreatedAt = model.CreatedAt
            };
        }
    }
}
