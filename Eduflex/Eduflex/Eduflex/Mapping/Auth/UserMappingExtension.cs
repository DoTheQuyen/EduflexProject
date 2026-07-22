using Eduflex.DTOs.Auth;
using ShareService.Models.Auth;

namespace Eduflex.Mapping.Auth
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
                RoleId = model.RoleId,
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
                RoleId = dto.RoleId,
                IsActive = dto.IsActive,
                LastLogin = dto.LastLogin
            };
        }

        public static CreateUserModel ToModel(this CreateUserDto dto)
        {
            return new CreateUserModel
            {
                Email = dto.Email,
                Password = dto.Password,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                RoleId = dto.RoleId
            };
        }
    }
}