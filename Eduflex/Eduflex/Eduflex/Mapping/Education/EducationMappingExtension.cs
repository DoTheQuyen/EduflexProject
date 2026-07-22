using Eduflex.DTOs.Education;
using ShareService.Models.Education;

namespace Eduflex.Mapping.Education
{
    public static class EducationMappingExtension
    {
        public static EducationDto ToDto(this EducationModel model)
        {
            return new EducationDto
            {
                Institution = model.Institution,
                Qualification = model.Qualification,
                YearCompleted = model.YearCompleted
            };
        }

        public static EducationModel ToModel(this EducationDto dto)
        {
            return new EducationModel
            {
                Institution = dto.Institution,
                Qualification = dto.Qualification,
                YearCompleted = dto.YearCompleted
            };
        }
    }
}