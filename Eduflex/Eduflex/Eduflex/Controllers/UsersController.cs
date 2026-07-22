using Eduflex.Authorization;
using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, IRoleService roleService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission(PermissionKeys.UsersView)]
        public async Task<ActionResult<List<UserSummaryDto>>> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            var roles = await _roleService.GetAllRolesAsync();
            var roleNameById = roles.ToDictionary(r => r.Id, r => r.Name);

            var result = users.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                RoleId = u.RoleId,
                RoleName = roleNameById.TryGetValue(u.RoleId ?? string.Empty, out var name) ? name : "Unknown",
                IsActive = u.IsActive,
                LastLogin = u.LastLogin
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        [RequirePermission(PermissionKeys.UsersAdd)]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(createDto.ToModel());
                return Ok(user.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUser endpoint");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }
    }
}
