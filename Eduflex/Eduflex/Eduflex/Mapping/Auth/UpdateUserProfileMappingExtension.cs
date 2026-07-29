using Eduflex.DTOs.Auth;
using ShareService.Models;
using ShareService.Models.Auth;

namespace Eduflex.Mapping.Auth
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
                Mobile = model.Mobile,
            };
        }

        public static UpdateUserProfileModel ToModel(this UpdateUserProfileDto dto)
        {
            return new UpdateUserProfileModel
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Mobile = dto.Mobile,
            };
        }
    }
}