using Eduflex.DTOs.Application;
using ShareService.Models.Application;

namespace Eduflex.Mapping.Application
{
    public static class CreateApplicationMappingExtension
    {
        public static ApplicationModel ToModel(this CreateApplicationDto dto)
        {
            return new ApplicationModel
            {
                StudentId = dto.StudentId,
                StudentName = dto.StudentName,
                Description = dto.Description,
                Details = dto.Details,
                ApplicationType = dto.ApplicationType
            };
        }
    }
}