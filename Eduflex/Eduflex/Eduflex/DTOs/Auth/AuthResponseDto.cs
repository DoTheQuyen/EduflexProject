namespace Eduflex.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool MustChangePassword { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}