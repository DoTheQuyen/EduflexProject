using Eduflex.API.DTOs;
using ShareService.Models.Auth;

namespace Eduflex.API.Mapping
{
    public static class UserMappingExtension
    {
        public static UserDto ToDto(this UserModel model)
        {
            return new UserDto
            {
                Id = model.Id,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = model.CreatedAt,
                Role = model.Role,
                IsActive = model.IsActive,
                LastLogin = model.LastLogin
            };
        }

        public static UserModel ToModel(this UserDto dto)
        {
            return new UserModel
            {
                Id = dto.Id,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = dto.CreatedAt,
                Role = dto.Role,
                IsActive = dto.IsActive,
                LastLogin = dto.LastLogin
            };
        }
    }
}