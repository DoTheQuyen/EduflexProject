using Eduflex.API.DTOs;
using ShareService.Models.Application;

namespace Eduflex.API.Mapping
{
    public static class ApplicationDetailMappingExtension
    {
        public static ApplicationDetailDto ToDto(this ApplicationDetailModel model)
        {
            return new ApplicationDetailDto
            {
                Id = model.Id,
                StudentId = model.StudentId,
                StudentName = model.StudentName,
                Description = model.Description,
                DateApplied = model.DateApplied,
                Status = model.Status,
                Details = model.Details,
                ApplicationType = model.ApplicationType
            };
        }

        public static ApplicationDetailModel ToModel(this ApplicationDetailDto dto)
        {
            return new ApplicationDetailModel
            {
                Id = dto.Id,
                StudentId = dto.StudentId,
                StudentName = dto.StudentName,
                Description = dto.Description,
                DateApplied = dto.DateApplied,
                Status = dto.Status,
                Details = dto.Details,
                ApplicationType = dto.ApplicationType
            };
        }
    }
}