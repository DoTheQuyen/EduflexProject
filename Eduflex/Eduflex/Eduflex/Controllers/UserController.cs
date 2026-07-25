using Eduflex.Authorization;
using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Models;
using ShareService.Services.Interface;
using System.Security.Claims;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "app")]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public Task<ActionResult<UserDto>> GetUserProfile()
        {
            return HandleRequestAsync(_logger, "Error getting user profile", async () =>
            {
                var userId = GetActingUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User not authenticated");

                var user = await _userService.GetUserByIdAsync(userId)
                    ?? throw new KeyNotFoundException("User not found");

                return user.ToDto();
            });
        }

        [HttpPut("profile")]
        public Task<ActionResult<UserDto>> UpdateUserProfile(UpdateUserProfileDto updateDto)
        {
            return HandleRequestAsync(_logger, "Error updating user profile", async () =>
            {
                var userId = GetActingUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User not authenticated");

                var updatedUser = await _userService.UpdateUserProfileAsync(userId, updateDto.ToModel())
                    ?? throw new KeyNotFoundException("User not found");

                return new UserDto
                {
                    Id = updatedUser.Id,
                    Email = updatedUser.Email,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    RoleId = updatedUser.RoleId,
                    CreatedAt = updatedUser.CreatedAt,
                    LastLogin = updatedUser.LastLogin,
                    IsActive = updatedUser.IsActive
                };
            });
        }

        [HttpPost("change-password")]
        [SkipMustChangePasswordCheck]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated");

                var result = await _userService.ChangePasswordAsync(userId, changePasswordDto.ToModel());
                if (!result)
                    return BadRequest("Current password is incorrect");

                return Ok("Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "An error occurred while changing password");
            }
        }
    }
}
