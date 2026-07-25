using ShareService.Common;
using ShareService.Models.Auth;
using ShareService.Services.Interface;

namespace ShareService.DataAccess.Interface
{
    public interface IUserDB
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByEmailAsync(string email);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash);
        Task<bool> CreateUserAsync(UserModel user);
        Task<bool> UpdateUserAsync(string id, UserModel user);
        Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter);
    }
}
