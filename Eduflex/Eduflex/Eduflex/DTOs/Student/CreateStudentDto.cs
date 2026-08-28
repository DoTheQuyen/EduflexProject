using Eduflex.DTOs.Address;
using ShareService.Enums.Student;
using System;

namespace Eduflex.DTOs.Student
{
    public class CreateStudentDto
    {
        public PersonType Type { get; set; } = PersonType.Student;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public AddressDto Address { get; set; } = new AddressDto();
    }
}
