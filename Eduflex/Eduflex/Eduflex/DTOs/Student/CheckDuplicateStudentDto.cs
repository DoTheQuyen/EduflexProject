using System;

namespace Eduflex.DTOs.Student
{
    public class CheckDuplicateStudentDto
    {
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PassportNumber { get; set; } = string.Empty;
    }
}
