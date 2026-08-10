using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Mapping;
using ShareService.Models.Role;
using ShareService.Services.Interface;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShareService.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRole _role;
        private readonly IPermissionCatalog _permissionCatalog;
        private readonly IValidator<RoleModel> _createRoleValidator;
        private readonly IPermissionService _permissionService;
        private readonly IUserDB _userDB;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRole role,
            IPermissionCatalog permissionCatalog,
            IValidator<RoleModel> createRoleValidator,
            IPermissionService permissionService,
            IUserDB userDB,
            ILogger<RoleService> logger)
        {
            _role = role;
            _permissionCatalog = permissionCatalog;
            _createRoleValidator = createRoleValidator;
            _permissionService = permissionService;
            _userDB = userDB;
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

        public async Task<RoleModel?> GetByNameAsync(string name)
        {
            try
            {
                return await _role.GetByNameAsync(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by name: {RoleName}", name);
                throw;
            }
        }

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

                var result = await _role.GetRolesAsync(query);

                var counts = await _userDB.CountUsersByRoleIdsAsync(result.Items.Select(r => r.Id));
                foreach (var role in result.Items)
                {
                    role.UserCount = counts.TryGetValue(role.Id, out var count) ? count : 0;
                }

                return result;
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

        // Auth: requires RolesAdd permission (staff-only).
        public async Task<bool> UpdateRoleAsync(string id, RoleModel role, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.RolesAdd, "create roles");

                //if (role.RoleType == Enums.Roles.RoleTypeEnums.Admin)
                //{
                //    throw new ArgumentException($"Not allow to create role type admin");
                //}

                var existing = await _role.GetByIdAsync(id);
                if (existing == null)
                {
                    throw new ArgumentException("Role not found");
                }

                var duplicate = await _role.GetByNameAsync(role.Name);
                if (duplicate != null && duplicate.Id != id)
                {
                    throw new ArgumentException($"A role named '{role.Name}' already exists");
                }

                var validate = await _createRoleValidator.ValidateAsync(role);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                existing.ApplyEditableFields(role);

                return await _role.UpdateAsync(id, existing);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleName}", role.Name);
                throw;
            }
        }
    }
}