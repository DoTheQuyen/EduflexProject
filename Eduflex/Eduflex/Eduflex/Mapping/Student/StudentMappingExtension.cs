using System.Linq;
using Eduflex.DTOs.Address;
using Eduflex.DTOs.Application;
using Eduflex.DTOs.Education;
using Eduflex.DTOs.Student;
using Eduflex.Mapping.Address;
using Eduflex.Mapping.Application;
using Eduflex.Mapping.Education;
using Eduflex.Mapping.Student;
using ShareService.Models.Student;

namespace Eduflex.Mapping.Student
{
    public static class StudentMappingExtension
    {
        public static StudentDto ToDto(this StudentModel model)
        {
            return new StudentDto
            {
                Id = model.Id,
                UserId = model.UserId,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Nationality = model.Nationality,
                PassportNumber = model.PassportNumber,
                DateOfBirth = model.DateOfBirth,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address?.ToDto() ?? new AddressDto(),
                EducationBackground = model.EducationBackground?.Select(eb => eb.ToDto()).ToList() ?? new List<EducationDto>(),
                Applications = model.Applications?.Select(app => app.ToDto()).ToList() ?? new List<ApplicationDto>(),
                CreatedAt = model.CreatedAt
            };
        }

        public static StudentModel ToModel(this StudentDto dto)
        {
            return new StudentModel
            {
                Id = dto.Id,
                UserId = dto.UserId,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nationality = dto.Nationality,
                PassportNumber = dto.PassportNumber,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address.ToModel(),
                EducationBackground = dto.EducationBackground.Select(eb => eb.ToModel()).ToList(),
                Applications = dto.Applications.Select(app => app.ToModel()).ToList(),
                CreatedAt = dto.CreatedAt
            };
        }
    }
}