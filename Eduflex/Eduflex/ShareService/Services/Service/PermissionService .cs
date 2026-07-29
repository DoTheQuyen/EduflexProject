using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShareService.DataAccess.Interface;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        // Deliberately depends on the DataAccess layer (IUserDB/IRole/IPermissionCatalog)
        // rather than IUserService/IRoleService: this service resolves permissions for
        // EVERY other service's authorization check, including UserService's and
        // RoleService's own — depending on those Service interfaces here would create a
        // circular dependency the DI container can't construct.
        private readonly IUserDB _userDB;
        private readonly IRole _roleDataAccess;
        private readonly IPermissionCatalog _permissionCatalog;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IUserDB userDB,
            IRole roleDataAccess,
            IPermissionCatalog permissionCatalog,
            IMemoryCache cache,
            ILogger<PermissionService> logger)
        {
            _userDB = userDB;
            _roleDataAccess = roleDataAccess;
            _permissionCatalog = permissionCatalog;
            _cache = cache;
            _logger = logger;
        }

        // Auth: N/A — this method IS the authorization primitive every other service's
        // "Auth:" check calls into. Returns an empty list (never throws) for an unknown
        // or deactivated user, so callers naturally fail their own permission checks
        // rather than needing special-case handling here.
        public async Task<List<string>> GetPermissionsForUserAsync(string userId)
        {
            var cacheKey = $"permissions:{userId}";

            if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var user = await _userDB.GetUserByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return new List<string>();
                }

                var role = await _roleDataAccess.GetByIdAsync(user.RoleId);
                if (role == null || role.PermissionIds == null || !role.PermissionIds.Any())
                {
                    return new List<string>();
                }

                var permissionModels = await _permissionCatalog.GetByIdsAsync(role.PermissionIds);
                var permissions = permissionModels.Select(p => p.Key).ToList();

                _cache.Set(cacheKey, permissions, CacheDuration);

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving permissions for user: {UserId}", userId);
                return new List<string>();
            }
        }
    }
}
