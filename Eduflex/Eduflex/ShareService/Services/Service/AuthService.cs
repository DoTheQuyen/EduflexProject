using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Models;
using ShareService.Services.Interface;

public class AuthService : IAuthService
{
    private readonly IAuthentication _authentication;
    private readonly IValidator<LoginModel> _validator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthentication authentication, IValidator<LoginModel> validator, ILogger<AuthService> logger)
    {
        _authentication = authentication;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UserModel?> ValidateUserAsync(LoginModel loginModel, Func<string, string, bool> verifyPassword)
    {
        try
        {
            // Create login model for validation
            //var loginModel = new LoginModel { Email = email, Password = password };

            // Use FluentValidation to validate rules of input object before process
            var validate = await _validator.ValidateAsync(loginModel);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            // Authorization here

            // Process business rule here
            var user = await _authentication.FindByEmailAsync(loginModel.Email);
            if (user == null)
            {
                _logger.LogInformation("User not found for email: {email}", loginModel.Email);
                return null;
            }

            var isValid = verifyPassword(loginModel.Password, user.PasswordHash);
            _logger.LogInformation("User validation result for {email}: {result}", loginModel.Email, isValid ? "Success" : "Failed");

            return isValid ? user : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateUserAsync for email: {email}", loginModel.Email);
            throw new Exception("Error in ValidateUserAsync", ex);
        }
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        try
        {
            // Process business rule here
            await _authentication.UpdateLastLoginAsync(userId);
            _logger.LogInformation("Last login updated for user: {userId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateLastLoginAsync for user: {userId}", userId);
            throw new Exception("Error in UpdateLastLoginAsync", ex);
        }
    }
}
