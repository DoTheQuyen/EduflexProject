using Eduflex.DTOs.Enrolment;
using Eduflex.Mapping.Address;
using Eduflex.Mapping.DynamicForm;
using ShareService.Models.Enrolment;

namespace Eduflex.Mapping.Enrolment
{
    public static class EnrolmentMappingExtension
    {
        public static ShareService.Models.Enrolment.EnrolmentFilter ToFilter(this EnrolmentFilterDto dto, string actingUserId)
        {
            return new ShareService.Models.Enrolment.EnrolmentFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm,
                Statuses = dto.Statuses,
                OwnerUserId = dto.MineOnly ? actingUserId : null
            };
        }

        public static EnrolmentModel ToModel(this CreateEnrolmentDto dto)
        {
            return new EnrolmentModel
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Email = dto.Email,
                Mobile = dto.Mobile,
                Nationality = dto.Nationality,
                PassportNumber = dto.PassportNumber,
                HometownAddress = dto.HometownAddress?.ToModel(),
                CurrentAddress = dto.CurrentAddress?.ToModel(),
                EmergencyContact = dto.EmergencyContact == null ? null : new EmergencyContactModel
                {
                    Name = dto.EmergencyContact.Name,
                    Relationship = dto.EmergencyContact.Relationship,
                    Phone = dto.EmergencyContact.Phone,
                    Email = dto.EmergencyContact.Email
                },
                EducationPartnerId = dto.EducationPartnerId,
                CourseId = dto.CourseId,
                Intake = dto.Intake,
                StudyMode = dto.StudyMode,
                Campus = dto.Campus,
                CommencementDate = dto.CommencementDate,
                ExpectedCompletionDate = dto.ExpectedCompletionDate,
                FundingSource = dto.FundingSource,
                VisaStatus = dto.VisaStatus,
                Notes = dto.Notes
            };
        }

        /// <summary>
        /// Applies the editable-field subset of an EnrolmentDto onto a fresh model for UpdateEnrolmentAsync —
        /// ownership/linkage fields are intentionally left at their defaults since the service layer
        /// only reads the editable fields via EnrolmentModel.ApplyEditableFields.
        /// </summary>
        public static EnrolmentModel ToModel(this EnrolmentDto dto)
        {
            return new EnrolmentModel
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Email = dto.Email,
                Mobile = dto.Mobile,
                Nationality = dto.Nationality,
                PassportNumber = dto.PassportNumber,
                HometownAddress = dto.HometownAddress?.ToModel(),
                CurrentAddress = dto.CurrentAddress?.ToModel(),
                EmergencyContact = dto.EmergencyContact == null ? null : new EmergencyContactModel
                {
                    Name = dto.EmergencyContact.Name,
                    Relationship = dto.EmergencyContact.Relationship,
                    Phone = dto.EmergencyContact.Phone,
                    Email = dto.EmergencyContact.Email
                },
                EducationPartnerId = dto.EducationPartnerId,
                CourseId = dto.CourseId,
                Intake = dto.Intake,
                StudyMode = dto.StudyMode,
                Campus = dto.Campus,
                CommencementDate = dto.CommencementDate,
                ExpectedCompletionDate = dto.ExpectedCompletionDate,
                FundingSource = dto.FundingSource,
                VisaStatus = dto.VisaStatus,
                Status = dto.Status,
                Notes = dto.Notes
            };
        }

        public static EnrolmentDto ToDto(this EnrolmentModel model, string ownerName)
        {
            return new EnrolmentDto
            {
                Id = model.Id,
                OwnerUserId = model.OwnerUserId,
                OwnerName = ownerName,
                StudentUserId = model.StudentUserId,
                StudentApplicationId = model.StudentApplicationId,
                EnquiryId = model.EnquiryId,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Email = model.Email,
                Mobile = model.Mobile,
                Nationality = model.Nationality,
                PassportNumber = model.PassportNumber,
                HometownAddress = model.HometownAddress?.ToDto(),
                CurrentAddress = model.CurrentAddress?.ToDto(),
                EmergencyContact = model.EmergencyContact == null ? null : new EmergencyContactDto
                {
                    Name = model.EmergencyContact.Name,
                    Relationship = model.EmergencyContact.Relationship,
                    Phone = model.EmergencyContact.Phone,
                    Email = model.EmergencyContact.Email
                },
                EducationPartnerId = model.EducationPartnerId,
                CourseId = model.CourseId,
                Intake = model.Intake,
                StudyMode = model.StudyMode,
                Campus = model.Campus,
                CommencementDate = model.CommencementDate,
                ExpectedCompletionDate = model.ExpectedCompletionDate,
                FundingSource = model.FundingSource,
                VisaStatus = model.VisaStatus,
                Status = model.Status,
                Notes = model.Notes,
                Documents = model.Documents.Select(d => new EnrolmentDocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    Category = d.Category,
                    Url = d.Url,
                    ContentType = d.ContentType,
                    SizeBytes = d.SizeBytes,
                    UploadedByUserId = d.UploadedByUserId,
                    UploadedByName = d.UploadedByName,
                    IsFromStudent = d.IsFromStudent,
                    UploadedAt = d.UploadedAt
                }).OrderByDescending(d => d.UploadedAt).ToList(),
                Communications = model.Communications.Select(c => new EnrolmentCommunicationDto
                {
                    Id = c.Id,
                    TemplateKey = c.TemplateKey,
                    ToEmail = c.ToEmail,
                    RecipientType = c.RecipientType,
                    Subject = c.Subject,
                    Body = c.Body,
                    AttachedDocumentIds = c.AttachedDocumentIds,
                    SentByName = c.SentByName,
                    SentAt = c.SentAt
                }).OrderByDescending(c => c.SentAt).ToList(),
                FormResponses = model.FormResponses.Select(f => f.ToDto()).OrderByDescending(f => f.RequestedAt).ToList(),
                AuditTrail = model.AuditTrail.Select(a => new EnrolmentAuditEntryDto
                {
                    Id = a.Id,
                    Description = a.Description,
                    PerformedByName = a.PerformedByName,
                    PerformedAt = a.PerformedAt
                }).OrderByDescending(a => a.PerformedAt).ToList(),
                // Not reordered like Documents/Communications/AuditTrail above — the list
                // order here IS the workflow order the VISA Process tab renders steps in.
                VisaProcessSteps = model.VisaProcessSteps.Select(s => new VisaProcessStepDto
                {
                    Key = s.Key,
                    Status = s.Status,
                    Fields = s.Fields,
                    CompletedByName = s.CompletedByName,
                    CompletedAt = s.CompletedAt
                }).ToList(),
                CourseApplications = model.CourseApplications.Select(c => c.ToDto()).OrderBy(c => c.CreatedAt).ToList(),
                MaxApplicationsOverride = model.MaxApplicationsOverride,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        public static EnrolmentDocumentModel ToModel(this AddEnrolmentDocumentDto dto)
        {
            return new EnrolmentDocumentModel
            {
                FileName = dto.FileName,
                Category = dto.Category,
                Url = dto.Url,
                ContentType = dto.ContentType,
                SizeBytes = dto.SizeBytes
            };
        }

        public static CourseApplicationDto ToDto(this CourseApplicationModel model)
        {
            return new CourseApplicationDto
            {
                Id = model.Id,
                EducationPartnerId = model.EducationPartnerId,
                CourseId = model.CourseId,
                Status = model.Status,
                Intake = model.Intake,
                StudyMode = model.StudyMode,
                Campus = model.Campus,
                CommencementDate = model.CommencementDate,
                ExpectedCompletionDate = model.ExpectedCompletionDate,
                ActualCommencementDate = model.ActualCommencementDate,
                Notes = model.Notes,
                TuitionFee = model.TuitionFee,
                CreatedAt = model.CreatedAt,
                StatusUpdatedAt = model.StatusUpdatedAt,
                StatusUpdatedByName = model.StatusUpdatedByName
            };
        }

        public static ShareService.Models.Enrolment.CourseApplicationDetailsModel ToModel(this UpdateCourseApplicationDetailsDto dto)
        {
            return new ShareService.Models.Enrolment.CourseApplicationDetailsModel
            {
                Intake = dto.Intake,
                StudyMode = dto.StudyMode,
                Campus = dto.Campus,
                CommencementDate = dto.CommencementDate,
                ExpectedCompletionDate = dto.ExpectedCompletionDate,
                ActualCommencementDate = dto.ActualCommencementDate,
                TuitionFee = dto.TuitionFee,
                Notes = dto.Notes
            };
        }
    }
}
