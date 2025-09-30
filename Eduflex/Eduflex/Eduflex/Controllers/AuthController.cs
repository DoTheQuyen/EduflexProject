using Eduflex.API.DTOs;
using Eduflex.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ShareService.Models;
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

        public AuthController(
            IConfiguration configuration, 
            IAuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        //[HttpPost("register")]
        //public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        //{
        //    // Check if user already exists
        //    var existingUser = await _mongoDBService.Users
        //        .Find(u => u.Email == registerDto.Email)
        //        .FirstOrDefaultAsync();

        //    if (existingUser != null)
        //        return BadRequest("User already exists");

        //    // Create new user
        //    var user = new User
        //    {
        //        Email = registerDto.Email,
        //        PasswordHash = HashPassword(registerDto.Password),
        //        FirstName = registerDto.FirstName,
        //        LastName = registerDto.LastName,
        //        CreatedAt = DateTime.UtcNow,
        //        Role = "Student"
        //    };

        //    await _mongoDBService.Users.InsertOneAsync(user);

        //    // Create student profile
        //    var student = new Student
        //    {
        //        UserId = user.Id,
        //        Nationality = registerDto.Nationality,
        //        //DateOfBirth = registerDto.DateOfBirth,
        //        PhoneNumber = registerDto.PhoneNumber,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await _mongoDBService.Students.InsertOneAsync(student);

        //    //var token = GenerateJwtToken(user);

        //    return new AuthResponseDto
        //    {
        //        //Token = token,
        //        Email = user.Email,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        //Role = user.Role
        //    };
        //}
                
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
          
            var user = await _authService.ValidateUserAsync(loginDto.ToModel(), VerifyPassword);

            if (user == null)
                return Unauthorized("Invalid credentials");

            await _authService.UpdateLastLoginAsync(user.Id);

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            });
        }

        [HttpPost("logout")]
        public async Task<ActionResult<string>> Logout()
        {
            // Your logout logic here
            return Ok( "Logged out successfully" );
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

        //private string GenerateJwtToken(User user)
        private string GenerateJwtToken(UserModel user)
        {
            // Implement JWT token generation
            // This is a simplified version - use proper JWT library in production
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}