using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Role;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRole _role;
        private readonly IPermissionCatalog _permissionCatalog;
        private readonly IValidator<RoleModel> _createRoleValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRole role,
            IPermissionCatalog permissionCatalog,
            IValidator<RoleModel> createRoleValidator,
            IPermissionService permissionService,
            ILogger<RoleService> logger)
        {
            _role = role;
            _permissionCatalog = permissionCatalog;
            _createRoleValidator = createRoleValidator;
            _permissionService = permissionService;
            _logger = logger;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // Auth: none — deliberately open at this layer. Purely internal plumbing, called
        // by UserService/PermissionService/EnrolmentService to resolve role names/ids;
        // never exposed directly as a controller action of its own.
        public async Task<RoleModel?> GetByIdAsync(string roleId)
        {
            try
            {
                return await _role.GetByIdAsync(roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by ID: {RoleId}", roleId);
                throw;
            }
        }

        // Auth: none — internal plumbing only (not used by PermissionService itself
        // anymore, that resolves permissions via DataAccess directly to avoid a circular
        // dependency; this method remains for any other caller that needs a role's
        // resolved permission keys, e.g. a future "what can this role do" admin view).
        public async Task<List<string>> GetPermissionsAsync(string roleId)
        {
            var role = await GetByIdAsync(roleId);
            if (role == null || role.PermissionIds == null || !role.PermissionIds.Any())
            {
                return new List<string>();
            }

            var permissions = await _permissionCatalog.GetByIdsAsync(role.PermissionIds);
            return permissions.Select(p => p.Key).ToList();
        }

        // Auth: none — internal plumbing (e.g. UsersController's role-name lookup for
        // the search-users listing), not exposed as its own controller action.
        public async Task<List<RoleModel>> GetAllRolesAsync()
        {
            try
            {
                return await _role.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                throw;
            }
        }

        // Auth: requires RolesView permission (staff-only).
        public async Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.RolesView, "view roles");

                return await _role.GetRolesAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged roles");
                throw;
            }
        }

        // Auth: requires RolesAdd permission (staff-only).
        public async Task<bool> CreateRoleAsync(RoleModel role, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.RolesAdd, "create roles");

                var validate = await _createRoleValidator.ValidateAsync(role);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existing = await _role.GetByNameAsync(role.Name);
                if (existing != null)
                {
                    throw new ArgumentException($"A role named '{role.Name}' already exists");
                }

                role.Id = string.Empty;
                role.PermissionIds ??= new List<string>();

                return await _role.CreateAsync(role);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role: {RoleName}", role.Name);
                throw;
            }
        }
    }
}