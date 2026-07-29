using Eduflex.DTOs.Auth;
using Eduflex.Mapping.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
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
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;

        public AuthController(
            IConfiguration configuration,
            IAuthService authService,
            IRoleService roleService,
            IPermissionService permissionService)
        {
            _configuration = configuration;
            _authService = authService;
            _roleService = roleService;
            _permissionService = permissionService;
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
            var token = GenerateJwtToken(user, role?.Name ?? "Student");
            var permissions = await _permissionService.GetPermissionsForUserAsync(user.Id);

            return Ok(new AuthResponseDto
            {
                Token = token,
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
            // Your logout logic here
            return Ok("Logged out successfully");
        }

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

        private string GenerateJwtToken(UserModel user, string roleName)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);
            var expiryHours = int.TryParse(_configuration["JWT:ExpiryHours"], out var hours) ? hours : 12;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)
                }),

                Expires = DateTime.UtcNow.AddHours(expiryHours),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}