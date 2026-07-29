using Eduflex.DTOs.Enrolment;
using ShareService.Models.Enrolment;

namespace Eduflex.Mapping.Enrolment
{
    public static class EmailTemplateMappingExtension
    {
        public static EmailTemplateDto ToDto(this EmailTemplateModel model)
        {
            return new EmailTemplateDto
            {
                Id = model.Id,
                Key = model.Key,
                Name = model.Name,
                Subject = model.Subject,
                Body = model.Body,
                IsSystemDefault = model.IsSystemDefault
            };
        }

        public static EmailTemplateModel ToModel(this CreateEmailTemplateDto dto)
        {
            return new EmailTemplateModel
            {
                Key = dto.Key,
                Name = dto.Name,
                Subject = dto.Subject,
                Body = dto.Body
            };
        }

        public static EmailTemplateModel ToModel(this UpdateEmailTemplateDto dto)
        {
            return new EmailTemplateModel
            {
                Name = dto.Name,
                Subject = dto.Subject,
                Body = dto.Body
            };
        }
    }
}
