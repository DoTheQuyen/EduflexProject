using Eduflex.Authorization;
using Eduflex.DTOs.Role;
using Eduflex.Mapping.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums.Permissions;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionCatalog _permissionCatalog;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            IRoleService roleService,
            IPermissionCatalog permissionCatalog,
            IModuleCatalog moduleCatalog,
            ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _permissionCatalog = permissionCatalog;
            _moduleCatalog = moduleCatalog;
            _logger = logger;
        }

        [HttpPost("search-roles")]
        public Task<ActionResult<PagedResult<RoleDto>>> SearchRoles([FromBody] RoleFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search roles endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var result = await _roleService.GetRolesAsync(filterDto.ToFilter(), userId);
                return new PagedResult<RoleDto>
                {
                    Items = result.Items.Select(r => r.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpGet("permissions")]
        [RequirePermission(PermissionKey.RolesView)]
        public Task<ActionResult<List<PermissionDto>>> GetPermissionCatalog()
        {
            return HandleRequestAsync(_logger, "Error in GetPermissionCatalog endpoint", async () =>
            {
                var permissions = await _permissionCatalog.GetAllAsync();
                var modules = await _moduleCatalog.GetAllAsync();
                var moduleById = modules.ToDictionary(m => m.Id, m => m);

                return permissions
                    .Select(p => p.ToDto(moduleById.TryGetValue(p.ModuleId, out var module) ? module : null))
                    .ToList();
            });
        }

        [HttpPost]
        public Task<ActionResult<bool>> CreateRole(CreateRoleDto createDto)
        {
            return HandleCreateAsync(_logger, "Error in CreateRole endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _roleService.CreateRoleAsync(createDto.ToModel(), userId);
            });
        }

    }
}
