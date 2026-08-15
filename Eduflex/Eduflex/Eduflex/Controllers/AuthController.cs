using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Auth;
using ShareService.Services;
using ShareService.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "auth")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IRefreshTokenStore _refreshTokenStore;

        public AuthController(
            IConfiguration configuration,
            IAuthService authService,
            IUserService userService,
            IRoleService roleService,
            IPermissionService permissionService,
            IRefreshTokenStore refreshTokenStore)
        {
            _configuration = configuration;
            _authService = authService;
            _userService = userService;
            _roleService = roleService;
            _permissionService = permissionService;
            _refreshTokenStore = refreshTokenStore;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var user = await _authService.ValidateUserAsync(loginDto.ToModel(), VerifyPassword);

            if (user == null)
                return Unauthorized("Invalid credentials");

            await _authService.UpdateLastLoginAsync(user.Id);

            var role = await _roleService.GetByIdAsync(user.RoleId);
            var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id);

            await IssueTokensAsync(user, role?.Name ?? "Student");

            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                RoleName = role?.Name ?? "Student",
                MustChangePassword = user.MustChangePassword,
                Permissions = permissions
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refresh_token", out var rawToken) || string.IsNullOrEmpty(rawToken))
                return Unauthorized();

            var hash = HashRefreshToken(rawToken);
            var stored = await _refreshTokenStore.FindByHashAsync(hash);

            if (stored == null)
                return Unauthorized();

            if (stored.RevokedAt != null)
            {
                // A revoked token being presented again means it was likely stolen
                // and already used by someone else — kill every session for this user.
                await _refreshTokenStore.RevokeAllForUserAsync(stored.UserId);
                ClearAuthCookies();
                return Unauthorized("Session revoked");
            }

            if (stored.ExpiresAt < DateTime.UtcNow)
            {
                ClearAuthCookies();
                return Unauthorized("Session expired");
            }

            var user = await _userService.GetUserByIdAsync(stored.UserId);
            if (user == null || !user.IsActive)
            {
                ClearAuthCookies();
                return Unauthorized();
            }

            await _refreshTokenStore.RevokeAsync(stored.Id);

            var role = await _roleService.GetByIdAsync(user.RoleId);
            var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id);

            await IssueTokensAsync(user, role?.Name ?? "Student");

            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                RoleName = role?.Name ?? "Student",
                MustChangePassword = user.MustChangePassword,
                Permissions = permissions
            });
        }

        [HttpPost("logout")]
        public async Task<ActionResult<string>> Logout()
        {
            if (Request.Cookies.TryGetValue("refresh_token", out var rawToken) && !string.IsNullOrEmpty(rawToken))
            {
                var hash = HashRefreshToken(rawToken);
                var stored = await _refreshTokenStore.FindByHashAsync(hash);
                if (stored != null)
                {
                    await _refreshTokenStore.RevokeAsync(stored.Id);
                }
            }

            ClearAuthCookies();
            return Ok("Logged out successfully");
        }

        private async Task IssueTokensAsync(UserModel user, string roleName)
        {
            var accessMinutes = int.TryParse(_configuration["JWT:AccessTokenMinutes"], out var m) ? m : 15;
            var refreshDays = int.TryParse(_configuration["JWT:RefreshTokenDays"], out var d) ? d : 7;

            var accessToken = GenerateAccessToken(user, roleName, accessMinutes);
            var refreshTokenRaw = GenerateRefreshTokenValue();

            await _refreshTokenStore.CreateAsync(new RefreshTokenModel
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(refreshTokenRaw),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
            });

            SetAuthCookies(accessToken, refreshTokenRaw, accessMinutes, refreshDays);
        }

        private void SetAuthCookies(string accessToken, string refreshToken, int accessMinutes, int refreshDays)
        {
            var sameSite = ParseSameSite(_configuration["Cookies:SameSite"]);

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = sameSite,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(accessMinutes)
            });

            // Scoped to /api/Auth only — the browser won't attach it to every other
            // request, so a leak of some other endpoint's response can't expose it.
            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = sameSite,
                Path = "/api/Auth",
                Expires = DateTimeOffset.UtcNow.AddDays(refreshDays)
            });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/Auth" });
        }

        private static SameSiteMode ParseSameSite(string? value) => value?.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Strict
        };

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + _configuration["JWT:Salt"]);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }

        private string GenerateAccessToken(UserModel user, string roleName, int expiryMinutes)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)
                }),

                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string GenerateRefreshTokenValue()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string HashRefreshToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(bytes);
        }
    }
}