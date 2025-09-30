using ShareService.Models;

namespace ShareService.Services.Interface
{
    public interface IUserService
    {
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    }

    public class UpdateUserProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}