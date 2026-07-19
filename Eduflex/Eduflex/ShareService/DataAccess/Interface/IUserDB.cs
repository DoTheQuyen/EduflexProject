using Eduflex.API.DTOs;
using ShareService.Models;
using ShareService.Services.Interface;

namespace ShareService.DataAccess.Interface
{
    public interface IUserDB
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> GetUserByEmailAsync(string email);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateDto);
        Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash);
    }
}