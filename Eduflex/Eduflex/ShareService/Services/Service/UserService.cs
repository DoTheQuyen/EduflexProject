using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;
using ShareService.Services.Interface;
using System.Security.Cryptography;
using System.Text;
using ShareService.Services.Interface.Integration;
using ShareService.Services.Service.Integration;
using Microsoft.Extensions.Options;
using ShareService.Models.Setting;

namespace ShareService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDB _userDB;
        private readonly IValidator<UpdateUserProfileModel> _profileValidator;
        private readonly IValidator<ChangePasswordModel> _passwordValidator;
        private readonly IValidator<CreateUserModel> _createUserValidator;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAzureEmailService _emailService;
        private readonly WebURLSettings _appSettings;

        public UserService(
             IUserDB userDB,
             IValidator<UpdateUserProfileModel> profileValidator,
             IValidator<ChangePasswordModel> passwordValidator,
             IValidator<CreateUserModel> createUserValidator,
             ILogger<UserService> logger,
             IConfiguration configuration,
             IAzureEmailService emailService,
             IOptions<WebURLSettings> appSettings)
        {
            _userDB = userDB;
            _profileValidator = profileValidator;
            _passwordValidator = passwordValidator;
            _createUserValidator = createUserValidator;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _appSettings = appSettings.Value;
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

        public async Task<UserModel?> UpdateUserProfileAsync(string userId, UpdateUserProfileModel updateModel)
        {
            try
            {
                // Validate input
                var validationResult = await _profileValidator.ValidateAsync(updateModel);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                // Check if email is already taken by another user
                var existingUser = await _userDB.GetUserByEmailAsync(updateModel.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    throw new ArgumentException("Email is already taken by another user");
                }

                return await _userDB.UpdateUserProfileAsync(userId, updateModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordModel changePasswordModel)
        {
            try
            {
                // Validate input
                var validationResult = await _passwordValidator.ValidateAsync(changePasswordModel);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                // Verify current password
                var user = await _userDB.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                var currentPasswordHash = HashPassword(changePasswordModel.CurrentPassword);
                if (user.PasswordHash != currentPasswordHash)
                    return false;

                // Update password
                var newPasswordHash = HashPassword(changePasswordModel.NewPassword);
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

        public async Task<UserModel> CreateUserAsync(CreateUserModel createUserModel)
        {
            try
            {
                var validationResult = await _createUserValidator.ValidateAsync(createUserModel);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existingUser = await _userDB.GetUserByEmailAsync(createUserModel.Email);
                if (existingUser != null)
                {
                    throw new ArgumentException("A user with this email already exists");
                }

                var user = new UserModel
                {
                    Email = createUserModel.Email,
                    PasswordHash = HashPassword(createUserModel.Password),
                    FirstName = createUserModel.FirstName,
                    LastName = createUserModel.LastName,
                    RoleId = createUserModel.RoleId,
                    IsActive = true,
                    MustChangePassword = true
                };

                var createdUser = await _userDB.CreateUserAsync(user);

                try
                {
                    var (subject, html, plainText) = EmailTemplates.NewUserWelcome(
                        createdUser.FirstName,
                        createdUser.Email,
                        createUserModel.Password,
                        $"{_appSettings.FrontendBaseUrl}/login");

                    await _emailService.SendEmailAsync(createdUser.Email, subject, html, plainText);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "User {UserId} was created but welcome email failed to send", createdUser.Id);
                }

                return createdUser;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", createUserModel.Email);
                throw;
            }
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            try
            {
                return await _userDB.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }
    }
}