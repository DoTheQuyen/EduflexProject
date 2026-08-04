using System;
using Eduflex.DTOs.Department;

namespace Eduflex.DTOs.Auth
{
    public class UserSummaryDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public List<DepartmentBadgeDto> Departments { get; set; } = new();
    }
}
