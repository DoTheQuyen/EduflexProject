using Eduflex.DTOs.Application;
using Eduflex.Mapping.Address;
using ShareService.Models.Application;
using ShareService.Models.Enrolment;

namespace Eduflex.Mapping.Application
{
    public static class ApplicationDetailMappingExtension
    {
        public static ApplicationDetailDto ToDto(this ApplicationDetailModel model)
        {
            return new ApplicationDetailDto
            {
                Id = model.Id,
                StudentId = model.StudentId,
                StudentName = model.StudentName,
                Description = model.Description,
                DateApplied = model.DateApplied,
                Status = model.Status,
                Details = model.Details,
                ApplicationType = model.ApplicationType,
                StudyMode = model.StudyMode,
                Campus = model.Campus,
                HometownAddress = model.HometownAddress?.ToDto(),
                CurrentAddress = model.CurrentAddress?.ToDto(),
                EmergencyContact = model.EmergencyContact == null ? null : new Eduflex.DTOs.Enrolment.EmergencyContactDto
                {
                    Name = model.EmergencyContact.Name,
                    Relationship = model.EmergencyContact.Relationship,
                    Phone = model.EmergencyContact.Phone,
                    Email = model.EmergencyContact.Email
                }
            };
        }

        public static ApplicationDetailModel ToModel(this ApplicationDetailDto dto)
        {
            return new ApplicationDetailModel
            {
                Id = dto.Id,
                StudentId = dto.StudentId,
                StudentName = dto.StudentName,
                Description = dto.Description,
                DateApplied = dto.DateApplied,
                Status = dto.Status,
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