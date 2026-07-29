using ShareService.Common;
using ShareService.Models.Auth;

namespace ShareService.Services.Interface
{
    public interface IUserService
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel changePasswordDto);
        Task<bool> CreateUserAsync(UserModel user, string actingUserId);
        Task<bool> UpdateUserAsync(string userId, UserModel updateModel);
        Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter, string userId);

        Task RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
