using ShareService.Models.Auth;

namespace ShareService.Services.Interface
{
    public interface IUserService
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel changePasswordDto);
        Task<UserModel> CreateUserAsync(CreateUserModel createUserModel);
        Task<List<UserModel>> GetAllUsersAsync();
    }

    

    
}