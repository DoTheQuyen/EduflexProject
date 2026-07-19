using Eduflex.API.DTOs;
using ShareService.Models;

namespace Eduflex.API.Mapping
{
    public static class UpdateUserProfileMappingExtension
    {
        public static UpdateUserProfileDto ToDto(this UpdateUserProfileModel model)
        {
            return new UpdateUserProfileDto
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
            };
        }

        public static UpdateUserProfileModel ToModel(this UpdateUserProfileDto dto)
        {
            return new UpdateUserProfileModel
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
            };
        }
    }
}