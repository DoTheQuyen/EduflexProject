using Eduflex.DTOs.Institution;
using ShareService.Models;
using ShareService.Models.Institution;

namespace Eduflex.Mapping.Institution
{
    public static class InstitutionsMappingExtension
    {
        public static InstitutionsDto ToDto(this InstitutionsModel model)
        {
            return new InstitutionsDto
            {
                T = model.T,
                Id = model.Id,
                StateMachine = model.StateMachine
            };
        }

        public static InstitutionsModel ToModel(this InstitutionsDto dto)
        {
            return new InstitutionsModel
            {
                T = dto.T,
                Id = dto.Id,
                StateMachine = dto.StateMachine
            };
        }
    }
}