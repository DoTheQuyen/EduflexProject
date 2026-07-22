using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IUserService userService,
            IRoleService roleService,
            IMemoryCache cache,
            ILogger<PermissionService> logger)
        {
            _userService = userService;
            _roleService = roleService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<string>> GetPermissionsForUserAsync(string userId)
        {
            var cacheKey = $"permissions:{userId}";

            if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return new List<string>();
                }

                var permissions = await _roleService.GetPermissionsAsync(user.RoleId);

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