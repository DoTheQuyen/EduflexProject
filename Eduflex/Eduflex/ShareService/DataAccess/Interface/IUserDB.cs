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

        Task<UserModel?> GetUserByMobileAsync(string mobile);
        Task<List<UserModel>> GetUsersByIdsAsync(IEnumerable<string> ids);
        Task<List<string>> GetUserIdsByActiveStatusAsync(bool isActive);
        Task<List<string>> GetUserIdsByRoleIdAsync(string roleId);
        Task<bool> SetActiveStatusAsync(string userId, bool isActive);
        Task<bool> UpdateContactInfoAsync(string userId, string email, string mobile);
        Task<bool> SetPasswordResetTokenAsync(string userId, string token, DateTime expiry);
        Task<UserModel?> GetUserByResetTokenAsync(string token);
        Task<bool> CompletePasswordResetAsync(string userId, string newPasswordHash);
    }
}
