using ShareService.Common;
using ShareService.Models.Auth;

namespace ShareService.Services.Interface
{
    public interface IUserService
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel changePasswordDto);
        Task<bool> CreateUserAsync(UserModel user);
        Task<bool> UpdateUserAsync(string userId, UserModel updateModel);
        Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter);
    }
}
