using Eduflex.DTOs.Enquiry;
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
                Statuses = dto.Statuses
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
                Subject = string.IsNullOrWhiteSpace(dto.Subject) ? "General Enquiry" : dto.Subject,
                CoursePromotionId = dto.CoursePromotionId,
                RecaptchaToken = dto.RecaptchaToken
            };
        }

        public static EnquiryDto ToDto(this EnquiryModel model, IDictionary<string, string>? userNames = null)
        {
            string? Resolve(string? userId) =>
                userId != null && userNames != null && userNames.TryGetValue(userId, out var name) ? name : userId;

            return new EnquiryDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                Email = model.Email,
                Mobile = model.Mobile,
                Enquiry = model.Enquiry,
                Subject = model.Subject,
                CoursePromotionId = model.CoursePromotionId,
                Status = model.Status,
                Response = model.Response,
                CreatedAt = model.CreatedAt,
                UpdatedBy = Resolve(model.UpdatedBy),
                UpdatedAt = model.UpdatedAt,
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
                Subject = model.Subject,
                CoursePromotionId = model.CoursePromotionId,
                Status = model.Status,
                Response = model.Response,
                CreatedAt = model.CreatedAt,
            };
        }
    }
}