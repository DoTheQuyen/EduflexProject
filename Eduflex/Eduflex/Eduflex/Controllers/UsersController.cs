using Eduflex.Authorization;
using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Enums.Permissions;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class UsersController : BaseApiController
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

        [HttpPost("search-users")]
        [RequirePermission(PermissionKey.UsersView)]
        public Task<ActionResult<PagedResult<UserSummaryDto>>> SearchUsers([FromBody] UserFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search users endpoint", async () =>
            {
                var result = await _userService.GetUsersAsync(filterDto.ToFilter());
                var roles = await _roleService.GetAllRolesAsync();
                var roleNameById = roles.ToDictionary(r => r.Id, r => r.Name);

                var items = result.Items.Select(u => new UserSummaryDto
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

                return new PagedResult<UserSummaryDto>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPost]
        [RequirePermission(PermissionKey.UsersAdd)]
        public Task<ActionResult<bool>> CreateUser(CreateUserDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateUser endpoint", () => _userService.CreateUserAsync(createDto.ToModel()));
        }

        [HttpPut("{id}")]
        [RequirePermission(PermissionKey.UsersEdit)]
        public Task<ActionResult<bool>> UpdateUser(string id, UserDto updateDto)
        {
            return HandleUpdateAsync(_logger, $"Error updating user: {id}", async () =>
            {
                var actingUserId = GetActingUserId();
                if (string.IsNullOrEmpty(actingUserId))
                    throw new UnauthorizedAccessException("User not authenticated");

                return await _userService.UpdateUserAsync(actingUserId, updateDto.ToModel(id));
            });
        }
    }
}
