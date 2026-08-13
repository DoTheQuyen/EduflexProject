using Eduflex.DTOs.VisaProcess;
using ShareService.Models.VisaProcess;

namespace Eduflex.Mapping.VisaProcess
{
    public static class PractitionerTagMappingExtension
    {
        public static PractitionerTagDto ToDto(this PractitionerTagModel model)
        {
            return new PractitionerTagDto
            {
                Id = model.Id,
                Name = model.Name,
                ColorHex = model.ColorHex,
                Description = model.Description,
                Active = model.Active
            };
        }

        public static PractitionerTagModel ToModel(this SavePractitionerTagDto dto)
        {
            return new PractitionerTagModel
            {
                Name = dto.Name,
                ColorHex = dto.ColorHex,
                Description = dto.Description,
                Active = dto.Active
            };
        }
    }
}
