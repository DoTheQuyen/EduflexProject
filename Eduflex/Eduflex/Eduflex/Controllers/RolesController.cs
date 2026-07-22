using Eduflex.Authorization;
using Eduflex.DTOs.Role;
using Eduflex.Mapping.Role;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "app")]
    public class RolesController : ControllerBase
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

        [HttpGet]
        [RequirePermission(PermissionKeys.RolesView)]
        public async Task<ActionResult<List<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles.Select(r => r.ToDto()).ToList());
        }

        [HttpGet("permissions")]
        [RequirePermission(PermissionKeys.RolesView)]
        public async Task<ActionResult<List<PermissionDto>>> GetPermissionCatalog()
        {
            var permissions = await _permissionCatalog.GetAllAsync();
            var modules = await _moduleCatalog.GetAllAsync();
            var moduleById = modules.ToDictionary(m => m.Id, m => m);

            var result = permissions
                .Select(p => p.ToDto(moduleById.TryGetValue(p.ModuleId, out var module) ? module : null))
                .ToList();

            return Ok(result);
        }

        [HttpPost]
        [RequirePermission(PermissionKeys.RolesAdd)]
        public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleDto createDto)
        {
            try
            {
                var role = await _roleService.CreateRoleAsync(createDto.ToModel());
                return Ok(role.ToDto());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateRole endpoint");
                return StatusCode(500, "An error occurred while creating the role");
            }
        }
    }
}
