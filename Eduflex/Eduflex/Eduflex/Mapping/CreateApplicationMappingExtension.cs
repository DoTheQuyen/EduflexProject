using Eduflex.API.DTOs;
using ShareService.Models.Application;

namespace Eduflex.API.Mapping
{
    public static class CreateApplicationMappingExtension
    {
        public static CreateApplicationDto ToDto(this CreateApplicationModel model)
        {
            return new CreateApplicationDto
            {
                StudentId = model.StudentId,
                UserId = model.UserId,
                StudentName = model.StudentName,
                Description = model.Description,
                Details = model.Details,
                ApplicationType = model.ApplicationType
            };
        }

        public static CreateApplicationModel ToModel(this CreateApplicationDto dto)
        {
            return new CreateApplicationModel
            {
                StudentId = dto.StudentId,
                UserId = dto.UserId,
                StudentName = dto.StudentName,
                Description = dto.Description,
                Details = dto.Details,
                ApplicationType = dto.ApplicationType
            };
        }
    }
}