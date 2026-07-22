using Eduflex.DTOs.Auth;
using ShareService.Models.Auth;

namespace Eduflex.Mapping.Auth
{
    public static class LoginMappingExtension
    {
        public static LoginDto ToDto(this LoginModel model)
        {
            return new LoginDto
            {
                Email = model.Email,
                Password = model.Password
            };
        }

        public static LoginModel ToModel(this LoginDto dto)
        {
            return new LoginModel
            {
                Email = dto.Email,
                Password = dto.Password
            };
        }
    }
}