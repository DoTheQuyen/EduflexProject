using Eduflex.API.DTOs;
using ShareService.Models;

namespace Eduflex.API.Mapping
{
    public static class CoursesMappingExtension
    {
        public static CoursesDto ToDto(this CoursesModel model)
        {
            return new CoursesDto
            {
                T = model.T,
                Id = model.Id,
                StateMachine = model.StateMachine
            };
        }

        public static CoursesModel ToModel(this CoursesDto dto)
        {
            return new CoursesModel
            {
                T = dto.T,
                Id = dto.Id,
                StateMachine = dto.StateMachine
            };
        }
    }
}