using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models;
using ShareService.Services.Interface;
using System.Security.Cryptography;
using System.Text;

namespace ShareService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDB _userDB;
        private readonly IValidator<UpdateUserProfileDto> _profileValidator;
        private readonly IValidator<ChangePasswordDto> _passwordValidator;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserDB userDB,
            IValidator<UpdateUserProfileDto> profileValidator,
            IValidator<ChangePasswordDto> passwordValidator,
            ILogger<UserService> logger,
            IConfiguration configuration)
        {
            _userDB = userDB;
            _profileValidator = profileValidator;
            _passwordValidator = passwordValidator;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            try
            {
                return await _userDB.GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileDto updateDto)
        {
            try
            {
                // Validate input
                var validationResult = await _profileValidator.ValidateAsync(updateDto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                // Check if email is already taken by another user
                var existingUser = await _userDB.GetUserByEmailAsync(updateDto.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    throw new ArgumentException("Email is already taken by another user");
                }

                return await _userDB.UpdateUserProfileAsync(userId, updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                // Validate input
                var validationResult = await _passwordValidator.ValidateAsync(changePasswordDto);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                // Verify current password
                var user = await _userDB.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                var currentPasswordHash = HashPassword(changePasswordDto.CurrentPassword);
                if (user.PasswordHash != currentPasswordHash)
                    return false;

                // Update password
                var newPasswordHash = HashPassword(changePasswordDto.NewPassword);
                return await _userDB.UpdatePasswordAsync(userId, newPasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                throw;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + _configuration["JWT:Salt"]);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}