using System;
using System.Collections.Generic;

namespace Eduflex.API.DTOs
{
    public class StudentDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public AddressDto Address { get; set; } = new AddressDto();
        public List<EducationDto> EducationBackground { get; set; } = new List<EducationDto>();
        public List<ApplicationDto> Applications { get; set; } = new List<ApplicationDto>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}