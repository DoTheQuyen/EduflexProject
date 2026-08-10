using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Mapping;
using ShareService.Models.Auth;
using ShareService.Models.Setting;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using ShareService.Services.Service.Integration;
using System.Security.Cryptography;
using System.Text;

namespace ShareService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDB _userDB;
        private readonly IValidator<UpdateUserProfileModel> _profileValidator;
        private readonly IValidator<ChangePasswordModel> _passwordValidator;
        private readonly IValidator<UserModel> _createUserValidator;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAzureEmailService _emailService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly WebURLSettings _appSettings;

        public UserService(
             IUserDB userDB,
             IValidator<UpdateUserProfileModel> profileValidator,
             IValidator<ChangePasswordModel> passwordValidator,
             IValidator<UserModel> createUserValidator,
             ILogger<UserService> logger,
             IConfiguration configuration,
             IAzureEmailService emailService,
             IRoleService roleService,
             IPermissionService permissionService,
             IOptions<WebURLSettings> appSettings)
        {
            _userDB = userDB;
            _profileValidator = profileValidator;
            _passwordValidator = passwordValidator;
            _createUserValidator = createUserValidator;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _roleService = roleService;
            _permissionService = permissionService;
            _appSettings = appSettings.Value;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // Auth: none — deliberately open at this layer. Used both for self-service
        // "my profile" (UserController, any authenticated user, userId is their own from
        // the token) and internally by other services to look up arbitrary users by id.
        // Not gated by a permission because the self-service case has no permission to
        // check against — the token IS the authorization for "my own" data.
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

        // Auth: none — self-service "update my own profile" (UserController), any
        // authenticated user, always acting on their own account (userId from the token).
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

        // Auth: none — self-service "change my own password", any authenticated user;
        // the current-password check inside is the real gate here, not a role permission.
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

        // Auth: requires UsersAdd permission (staff-only).
        public async Task<bool> CreateUserAsync(UserModel user, string actingUserId)
        {
            var plaintextPassword = user.Password;
            try
            {
                await RequirePermissionAsync(actingUserId, PermissionKey.UsersAdd, "create users");

                var validationResult = await _createUserValidator.ValidateAsync(user, options => options.IncludeRuleSets("default", "Create"));
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existingUser = await _userDB.GetUserByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    throw new ArgumentException("A user with this email already exists");
                }

                user.Id = string.Empty;
                user.PasswordHash = HashPassword(plaintextPassword);
                user.Password = string.Empty;
                user.IsActive = true;
                user.MustChangePassword = true;
                user.LastLogin = null;

                var created = await _userDB.CreateUserAsync(user);

                try
                {
                    var (subject, html, plainText) = EmailTemplates.NewUserWelcome(
                        user.FirstName,
                        user.Email,
                        plaintextPassword,
                        $"{_appSettings.FrontendBaseUrl}/login");

                    await _emailService.SendEmailAsync(user.Email, subject, html, plainText);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "User {UserId} was created but welcome email failed to send", user.Id);
                }

                return created;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", user.Email);
                throw;
            }
        }

        // Auth: requires UsersEdit permission (staff-only), PLUS the business rules below
        // (CheckCanModifyUserAsync/CheckNotEscalatingRoleAsync) — those are ownership/
        // escalation rules, not a substitute for the permission check.
        public async Task<bool> UpdateUserAsync(string userId, UserModel updateModel)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.UsersEdit, "update users");

                var validationResult = await _createUserValidator.ValidateAsync(updateModel);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existingUser = await _userDB.GetUserByIdAsync(updateModel.Id);
                if (existingUser == null)
                {
                    throw new ArgumentException("User not found");
                }

                await CheckCanModifyUserAsync(existingUser, userId, updateModel.IsActive);

                if (updateModel.RoleId != existingUser.RoleId)
                {
                    await CheckNotEscalatingRoleAsync(userId, updateModel.RoleId);
                }

                existingUser.ApplyEditableFields(updateModel);

                return await _userDB.UpdateUserAsync(existingUser.Id, existingUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {UserId}", updateModel.Id);
                throw;
            }
        }



        private async Task CheckCanModifyUserAsync(UserModel existingUser, string actingUserId, bool newActiveStatus)
        {
            if (existingUser.Id == actingUserId && !newActiveStatus)
            {
                throw new ArgumentException("Cannot deactivate your own account");
            }

            var existingRole = await _roleService.GetByIdAsync(existingUser.RoleId);
            if (existingUser.Id != actingUserId && existingRole?.RoleType == RoleTypeEnums.Admin)
            {
                throw new ArgumentException("Cannot update another admin's details");
            }
        }

        private async Task CheckNotEscalatingRoleAsync(string actingUserId, string targetRoleId)
        {
            var actingUser = await _userDB.GetUserByIdAsync(actingUserId);
            var actingRole = actingUser != null ? await _roleService.GetByIdAsync(actingUser.RoleId) : null;
            var targetRole = await _roleService.GetByIdAsync(targetRoleId);

            if (actingRole != null && targetRole != null &&
                targetRole.RoleType.IsHigherThan(actingRole.RoleType))
            {
                throw new ArgumentException("Cannot assign a role higher than your own");
            }
        }

        // Auth: requires UsersView permission (staff-only).
        public async Task<PagedResult<UserModel>> GetUsersAsync(UserFilter filter, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.UsersView, "view users");

                return await _userDB.GetUsersAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users");
                throw;
            }
        }

        // Auth: none — public "forgot password" entry point, unauthenticated by
        // definition (the caller doesn't have a session yet). Deliberately never
        // reveals whether the email exists: silently no-ops instead of throwing so
        // callers can't use this endpoint to enumerate registered accounts.
        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _userDB.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for unknown email: {Email}", email);
                return;
            }

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expiry = DateTime.UtcNow.AddHours(1);

            await _userDB.SetPasswordResetTokenAsync(user.Id, token, expiry);

            try
            {
                var resetLink = $"{_appSettings.FrontendBaseUrl}/reset-password?token={token}";
                var (subject, html, plainText) = EmailTemplates.PasswordReset(user.FirstName, resetLink);
                await _emailService.SendEmailAsync(user.Email, subject, html, plainText);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Password reset token was set for {UserId} but email failed to send", user.Id);
            }
        }

        // Auth: none — the reset token itself (single-use, 1-hour expiry) is the
        // authorization, same pattern as the "forgot password" step above.
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters");
            }

            var user = await _userDB.GetUserByResetTokenAsync(token);
            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                throw new ArgumentException("This reset link is invalid or has expired");
            }

            return await _userDB.CompletePasswordResetAsync(user.Id, HashPassword(newPassword));
        }
    }
}