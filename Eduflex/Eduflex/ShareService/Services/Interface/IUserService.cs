using Eduflex.API.DTOs;
using ShareService.Models;

namespace ShareService.Services.Interface
{
    public interface IUserService
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel changePasswordDto);
    }

    

    
}