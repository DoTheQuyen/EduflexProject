using Eduflex.DTOs.Student;
using Eduflex.Mapping.Address;
using ShareService.Models.Auth;
using ShareService.Models.Student;

namespace Eduflex.Mapping.Student
{
    public static class StudentAdminMappingExtension
    {
        public static StudentFilter ToFilter(this StudentFilterDto dto)
        {
            return new StudentFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm,
                IsActive = dto.IsActive,
                Type = dto.Type
            };
        }

        public static UserModel ToUserModel(this CreateStudentDto dto)
        {
            return new UserModel
            {
                Email = dto.Email,
                Mobile = dto.Mobile,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Password = GenerateTempPassword()
            };
        }

        public static StudentModel ToProfileModel(this CreateStudentDto dto)
        {
            return new StudentModel
            {
                Type = dto.Type,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nationality = dto.Nationality,
                PassportNumber = dto.PassportNumber,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.Mobile,
                Address = dto.Address.ToModel()
            };
        }

        public static StudentModel ToProfileModel(this UpdateStudentDto dto)
        {
            return new StudentModel
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nationality = dto.Nationality,
                PassportNumber = dto.PassportNumber,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.Mobile,
                Address = dto.Address.ToModel()
            };
        }

        public static StudentAccountDto ToDto(this StudentAccountModel model)
        {
            return new StudentAccountDto
            {
                Id = model.Student.Id,
                UserId = model.Student.UserId,
                Type = model.Student.Type,
                Email = model.Student.Email,
                Mobile = model.Mobile,
                FirstName = model.Student.FirstName,
                LastName = model.Student.LastName,
                Nationality = model.Student.Nationality,
                PassportNumber = model.Student.PassportNumber,
                DateOfBirth = model.Student.DateOfBirth,
                Address = model.Student.Address?.ToDto() ?? new Eduflex.DTOs.Address.AddressDto(),
                IsActive = model.IsActive,
                LastLogin = model.LastLogin,
                CreatedAt = model.Student.CreatedAt,
                AuditTrail = model.Student.AuditTrail.Select(a => new StudentAuditEntryDto
                {
                    Id = a.Id,
                    Description = a.Description,
                    PerformedByName = a.PerformedByName,
                    PerformedAt = a.PerformedAt
                }).OrderByDescending(a => a.PerformedAt).ToList()
            };
        }

        public static DuplicateCheckResultDto ToDto(this DuplicateCheckResult result)
        {
            return new DuplicateCheckResultDto
            {
                IsDuplicate = result.IsDuplicate,
                MatchedField = result.MatchedField,
                ExistingStudentId = result.ExistingStudentId,
                ExistingUserId = result.ExistingUserId,
                ExistingIsActive = result.ExistingIsActive
            };
        }

        private static string GenerateTempPassword() => "Ef-" + Guid.NewGuid().ToString("N")[..10];
    }
}
