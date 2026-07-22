using Eduflex.DTOs.Application;
using ShareService.Models.Application;

namespace Eduflex.Mapping.Application
{
    public static class ApplicationMappingExtension
    {
        public static ApplicationDto ToDto(this ApplicationModel model)
        {
            return new ApplicationDto
            {
                Id = model.Id,
                StudentId = model.StudentId,
                StudentName = model.StudentName,
                StudentEmail = model.StudentEmail,
                Description = model.Description,
                Details = model.Details,
                ApplicationType = model.ApplicationType,
                DateApplied = model.DateApplied,
                Status = model.Status,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        public static ApplicationModel ToModel(this ApplicationDto dto)
        {
            return new ApplicationModel
            {
                Id = dto.Id,
                StudentId = dto.StudentId,
                StudentName = dto.StudentName,
                StudentEmail = dto.StudentEmail,
                Description = dto.Description,
                Details = dto.Details,
                ApplicationType = dto.ApplicationType,
                DateApplied = dto.DateApplied,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }
    }
}