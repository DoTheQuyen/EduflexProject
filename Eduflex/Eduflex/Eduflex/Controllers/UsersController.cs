using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Eduflex.Mapping.Department;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IRoleService roleService,
            IDepartmentService departmentService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _roleService = roleService;
            _departmentService = departmentService;
            _logger = logger;
        }

        [HttpPost("search-users")]
        public Task<ActionResult<PagedResult<UserSummaryDto>>> SearchUsers([FromBody] UserFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search users endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var result = await _userService.GetUsersAsync(filterDto.ToFilter(), actingUserId);
                var roles = await _roleService.GetAllRolesAsync();
                var roleNameById = roles.ToDictionary(r => r.Id, r => r.Name);
                var departments = await _departmentService.GetAllDepartmentsAsync();

                var items = result.Items.Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Mobile = u.Mobile,
                    RoleId = u.RoleId,
                    RoleName = roleNameById.TryGetValue(u.RoleId ?? string.Empty, out var name) ? name : "Unknown",
                    IsActive = u.IsActive,
                    LastLogin = u.LastLogin,
                    Departments = departments.ToBadges(u.Id)
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
        public Task<ActionResult<bool>> CreateUser(CreateUserDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateUser endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();

                return _userService.CreateUserAsync(createDto.ToModel(), actingUserId);
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> UpdateUser(string id, UserDto updateDto)
        {
            return HandleUpdateAsync(_logger, $"Error updating user: {id}", async () =>
            {
                var actingUserId = GetRequiredUserId();

                return await _userService.UpdateUserAsync(actingUserId, updateDto.ToModel(id));
            });
        }
    }
}
