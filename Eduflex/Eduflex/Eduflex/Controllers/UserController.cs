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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated");

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                return Ok(user.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "An error occurred while retrieving user profile");
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateUserProfile(UpdateUserProfileDto updateDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not authenticated");

                var updatedUser = await _userService.UpdateUserProfileAsync(userId, updateDto.ToModel());
                if (updatedUser == null)
                    return NotFound("User not found");

                return Ok(new UserDto
                {
                    Id = updatedUser.Id,
                    Email = updatedUser.Email,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    RoleId = updatedUser.RoleId,
                    CreatedAt = updatedUser.CreatedAt,
                    LastLogin = updatedUser.LastLogin,
                    IsActive = updatedUser.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "An error occurred while updating user profile");
            }
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