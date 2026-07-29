using Eduflex.DTOs.Application;
using Eduflex.Mapping.Address;
using ShareService.Models.Application;
using ShareService.Models.Enrolment;

namespace Eduflex.Mapping.Application
{
    public static class CreateApplicationMappingExtension
    {
        public static ApplicationModel ToModel(this CreateApplicationDto dto)
        {
            return new ApplicationModel
            {
                StudentId = dto.StudentId,
                StudentName = dto.StudentName,
                Description = dto.Description,
                Details = dto.Details,
                ApplicationType = dto.ApplicationType,
                StudyMode = dto.StudyMode,
                Campus = dto.Campus,
                HometownAddress = dto.HometownAddress?.ToModel(),
                CurrentAddress = dto.CurrentAddress?.ToModel(),
                EmergencyContact = dto.EmergencyContact == null ? null : new EmergencyContactModel
                {
                    Name = dto.EmergencyContact.Name,
                    Relationship = dto.EmergencyContact.Relationship,
                    Phone = dto.EmergencyContact.Phone,
                    Email = dto.EmergencyContact.Email
                }
            };
        }
    }
}