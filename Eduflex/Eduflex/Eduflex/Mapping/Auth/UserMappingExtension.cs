using Eduflex.DTOs.Auth;
using ShareService.Common;
using ShareService.Models.Auth;

namespace Eduflex.Mapping.Auth
{
    public static class UserMappingExtension
    {
        public static UserFilter ToFilter(this UserFilterDto dto)
        {
            return new UserFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm,
                RoleId = dto.RoleId,
                IsActive = dto.IsActive
            };
        }

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

        public static UserModel ToModel(this CreateUserDto dto)
        {
            return new UserModel
            {
                Email = dto.Email,
                Password = dto.Password,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                RoleId = dto.RoleId,
            };
        }

        public static UserModel ToModel(this UserDto dto, string id)
        {
            return new UserModel
            {
                Id = id,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                RoleId = dto.RoleId,
                IsActive = dto.IsActive
            };
        }
    }
}