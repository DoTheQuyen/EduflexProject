using Eduflex.API.DTOs;
using ShareService.Models.Auth;

namespace Eduflex.API.Mapping
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