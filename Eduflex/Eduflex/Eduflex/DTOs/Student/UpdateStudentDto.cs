using Eduflex.DTOs.Address;
using System;

namespace Eduflex.DTOs.Student
{
    // No Type here deliberately — Student vs Customer is fixed at creation, an edit can't
    // flip it (see StudentModel.Type).
    public class UpdateStudentDto
    {
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
