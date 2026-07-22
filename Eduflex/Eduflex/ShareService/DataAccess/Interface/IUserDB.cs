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
        Task<UserModel> CreateUserAsync(UserModel user);
        Task<List<UserModel>> GetAllUsersAsync();
    }
}