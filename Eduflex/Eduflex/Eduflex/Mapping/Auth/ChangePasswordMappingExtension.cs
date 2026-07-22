using Eduflex.DTOs.Auth;
using ShareService.Models.Auth;

namespace Eduflex.Mapping.Auth
{
    public static class ChangePasswordMappingExtension
    {
        public static ChangePasswordDto ToDto(this ChangePasswordModel model)
        {
            return new ChangePasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword,
            };
        }

        public static ChangePasswordModel ToModel(this ChangePasswordDto dto)
        {
            return new ChangePasswordModel
            {
                CurrentPassword = dto.CurrentPassword,
                NewPassword = dto.NewPassword,
                ConfirmPassword = dto.ConfirmPassword,
            };
        }
    }
}