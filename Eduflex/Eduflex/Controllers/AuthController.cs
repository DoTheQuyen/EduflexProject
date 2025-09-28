using Eduflex.API.DTOs;
using Eduflex.API.Models;
using Eduflex.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
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
        private readonly MongoDBService _mongoDBService;
        private readonly IConfiguration _configuration;

        public AuthController(MongoDBService mongoDBService, IConfiguration configuration)
        {
            _mongoDBService = mongoDBService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _mongoDBService.Users
                .Find(u => u.Email == registerDto.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return BadRequest("User already exists");

            // Create new user
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                CreatedAt = DateTime.UtcNow,
                Role = "Student"
            };

            await _mongoDBService.Users.InsertOneAsync(user);

            // Create student profile
            var student = new Student
            {
                UserId = user.Id,
                Nationality = registerDto.Nationality,
                //DateOfBirth = registerDto.DateOfBirth,
                PhoneNumber = registerDto.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            await _mongoDBService.Students.InsertOneAsync(student);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                //Role = user.Role
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var user = await _mongoDBService.Users
                .Find(u => u.Email == loginDto.Email)
                .FirstOrDefaultAsync();

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            // Update last login
            var update = Builders<User>.Update.Set(u => u.LastLogin, DateTime.UtcNow);
            await _mongoDBService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            };
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

        private string GenerateJwtToken(User user)
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