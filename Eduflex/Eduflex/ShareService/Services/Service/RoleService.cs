using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Role;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRole _role;
        private readonly IPermissionCatalog _permissionCatalog;
        private readonly IValidator<RoleModel> _createRoleValidator;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            IRole role,
            IPermissionCatalog permissionCatalog,
            IValidator<RoleModel> createRoleValidator,
            ILogger<RoleService> logger)
        {
            _role = role;
            _permissionCatalog = permissionCatalog;
            _createRoleValidator = createRoleValidator;
            _logger = logger;
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

        public async Task<PagedResult<RoleModel>> GetRolesAsync(PaginationQuery query)
        {
            try
            {
                return await _role.GetRolesAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged roles");
                throw;
            }
        }

        public async Task<bool> CreateRoleAsync(RoleModel role)
        {
            try
            {
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