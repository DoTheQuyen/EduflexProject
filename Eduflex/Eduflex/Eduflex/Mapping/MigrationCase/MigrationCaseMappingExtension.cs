using Eduflex.DTOs.Enrolment;
using Eduflex.DTOs.MigrationCase;
using Eduflex.Mapping.Address;
using Eduflex.Mapping.DynamicForm;
using Eduflex.Mapping.VisaProcess;
using ShareService.Models.Enrolment;
using ShareService.Models.MigrationCase;

namespace Eduflex.Mapping.MigrationCase
{
    public static class MigrationCaseMappingExtension
    {
        public static MigrationCaseFilter ToFilter(this MigrationCaseFilterDto dto)
        {
            return new MigrationCaseFilter
            {
                Statuses = dto.Statuses,
                OwnerUserId = dto.OwnerUserId,
                Category = dto.Category,
                SearchTerm = dto.SearchTerm,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize
            };
        }

        public static MigrationCaseDto ToDto(this MigrationCaseModel model)
        {
            return new MigrationCaseDto
            {
                Id = model.Id,
                OwnerUserId = model.OwnerUserId,
                CaseReference = model.CaseReference,
                TemplateId = model.TemplateId,
                TemplateVersion = model.TemplateVersion,
                TemplateName = model.TemplateName,
                Country = model.Country,
                Category = model.Category,
                PrimaryContactName = model.PrimaryContactName,
                PrimaryContactEmail = model.PrimaryContactEmail,
                PrimaryContactMobile = model.PrimaryContactMobile,
                MiddleName = model.MiddleName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
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
                Notes = model.Notes,
                Status = model.Status,
                Steps = model.Steps.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList(),
                Documents = model.Documents.OrderByDescending(d => d.UploadedAt).Select(d => d.ToDto()).ToList(),
                Communications = model.Communications.OrderByDescending(c => c.SentAt).Select(c => c.ToDto()).ToList(),
                FormResponses = model.FormResponses.OrderByDescending(f => f.RequestedAt).Select(f => f.ToMigrationCaseDto()).ToList(),
                AuditTrail = model.AuditTrail.OrderByDescending(a => a.PerformedAt).Select(a => a.ToDto()).ToList(),
                CreatedAt = model.CreatedAt
            };
        }

        public static MigrationCaseStepDto ToDto(this MigrationCaseStepModel model)
        {
            return new MigrationCaseStepDto
            {
                Key = model.Key,
                Order = model.Order,
                Label = model.Label,
                Description = model.Description,
                Phase = model.Phase,
                CanReopen = model.CanReopen,
                PractitionerTagId = model.PractitionerTagId,
                // Reuses StepFieldDefinitionModel/StepPreconditionModel/ProcessStepHintModel's
                // .ToDto() extensions from Eduflex.Mapping.VisaProcess — same shared shapes,
                // not duplicated here.
                FieldsSnapshot = model.FieldsSnapshot.Select(f => f.ToDto()).ToList(),
                RequiredEvidenceCategoriesSnapshot = model.RequiredEvidenceCategoriesSnapshot,
                PreconditionsSnapshot = model.PreconditionsSnapshot.Select(p => p.ToDto()).ToList(),
                HintsSnapshot = model.HintsSnapshot.OrderByDescending(h => h.Pinned).ThenByDescending(h => h.CreatedAt).Select(h => h.ToDto()).ToList(),
                Status = model.Status,
                Fields = model.Fields,
                CompletedAt = model.CompletedAt,
                CompletedByName = model.CompletedByName
            };
        }

        public static MigrationCaseDocumentDto ToDto(this MigrationCaseDocumentModel model)
        {
            return new MigrationCaseDocumentDto
            {
                Id = model.Id,
                FileName = model.FileName,
                Category = model.Category,
                Note = model.Note,
                Url = model.Url,
                ContentType = model.ContentType,
                SizeBytes = model.SizeBytes,
                UploadedByName = model.UploadedByName,
                UploadedAt = model.UploadedAt
            };
        }

        public static MigrationCaseCommunicationDto ToDto(this EnrolmentCommunicationModel model)
        {
            return new MigrationCaseCommunicationDto
            {
                Id = model.Id,
                TemplateKey = model.TemplateKey,
                ToEmail = model.ToEmail,
                RecipientType = model.RecipientType,
                Subject = model.Subject,
                Body = model.Body,
                AttachedDocumentIds = model.AttachedDocumentIds,
                SentByName = model.SentByName,
                SentAt = model.SentAt
            };
        }

        // Named ToMigrationCaseDto rather than ToDto — EnrolmentFormResponseModel already
        // has a ToDto() extension (Eduflex.Mapping.DynamicForm.
        // EnrolmentFormResponseMappingExtension, returning EnrolmentFormResponseDto) that
        // would otherwise collide, since C# can't overload extension resolution on return
        // type alone.
        public static MigrationCaseFormResponseDto ToMigrationCaseDto(this EnrolmentFormResponseModel model)
        {
            return new MigrationCaseFormResponseDto
            {
                Id = model.Id,
                FormTemplateId = model.FormTemplateId,
                FormName = model.FormName,
                QuestionsSnapshot = model.QuestionsSnapshot.OrderBy(q => q.Order).Select(q => q.ToDto()).ToList(),
                BoundStepKey = model.BoundStepKey,
                Status = model.Status,
                Answers = model.Answers.Select(a => a.ToDto()).ToList(),
                RequestedByName = model.RequestedByName,
                RequestedAt = model.RequestedAt,
                LastSavedAt = model.LastSavedAt,
                SubmittedAt = model.SubmittedAt,
                WithdrawnAt = model.WithdrawnAt,
                StaffEditedAt = model.StaffEditedAt,
                ExportedDocumentId = model.ExportedDocumentId
            };
        }

        public static List<FormAnswerModel> ToModel(this SaveMigrationCaseFormAnswersDto dto)
        {
            return dto.Answers.Select(a => new FormAnswerModel
            {
                QuestionId = a.QuestionId,
                TextValue = a.TextValue,
                SelectedOptions = a.SelectedOptions
            }).ToList();
        }

        public static MigrationCaseAuditEntryDto ToDto(this MigrationCaseAuditEntryModel model)
        {
            return new MigrationCaseAuditEntryDto
            {
                Id = model.Id,
                Description = model.Description,
                PerformedByName = model.PerformedByName,
                PerformedAt = model.PerformedAt
            };
        }

        public static MigrationCaseModel ToModel(this UpdateCustomerInfoDto dto)
        {
            return new MigrationCaseModel
            {
                PrimaryContactName = dto.PrimaryContactName,
                PrimaryContactEmail = dto.PrimaryContactEmail,
                PrimaryContactMobile = dto.PrimaryContactMobile,
                MiddleName = dto.MiddleName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
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
                }
            };
        }

        public static MigrationCaseDocumentModel ToModel(this AddMigrationCaseDocumentDto dto)
        {
            return new MigrationCaseDocumentModel
            {
                FileName = dto.FileName,
                Category = dto.Category,
                Note = dto.Note,
                Url = dto.Url,
                ContentType = dto.ContentType,
                SizeBytes = dto.SizeBytes
            };
        }
    }
}
