using Eduflex.DTOs.VisaProcess;
using ShareService.Models.VisaProcess;

namespace Eduflex.Mapping.VisaProcess
{
    public static class VisaProcessTemplateMappingExtension
    {
        public static VisaProcessTemplateDto ToDto(this VisaProcessTemplateModel model)
        {
            return new VisaProcessTemplateDto
            {
                Id = model.Id,
                Name = model.Name,
                Country = model.Country,
                Category = model.Category,
                Description = model.Description,
                Status = model.Status,
                IsDefaultForCountry = model.IsDefaultForCountry,
                Version = model.Version,
                Steps = model.Steps.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList()
            };
        }

        public static VisaProcessStepDefinitionDto ToDto(this VisaProcessStepDefinitionModel model)
        {
            return new VisaProcessStepDefinitionDto
            {
                Key = model.Key,
                Order = model.Order,
                Label = model.Label,
                Description = model.Description,
                Phase = model.Phase,
                Enabled = model.Enabled,
                CanReopen = model.CanReopen,
                PractitionerTagId = model.PractitionerTagId,
                Fields = model.Fields.Select(f => f.ToDto()).ToList(),
                RequiredEvidenceCategories = model.RequiredEvidenceCategories,
                Preconditions = model.Preconditions.Select(p => p.ToDto()).ToList(),
                SetsEnrolmentStatusTo = model.SetsEnrolmentStatusTo,
                Hints = model.Hints.OrderByDescending(h => h.Pinned).ThenByDescending(h => h.CreatedAt).Select(h => h.ToDto()).ToList()
            };
        }

        public static StepFieldDefinitionDto ToDto(this StepFieldDefinitionModel model)
        {
            return new StepFieldDefinitionDto
            {
                FieldKey = model.FieldKey,
                Label = model.Label,
                InputType = model.InputType,
                Options = model.Options,
                IsRequired = model.IsRequired
            };
        }

        public static StepPreconditionDto ToDto(this StepPreconditionModel model)
        {
            return new StepPreconditionDto
            {
                Type = model.Type,
                SourceStepKey = model.SourceStepKey,
                FieldKey = model.FieldKey,
                AllowedValues = model.AllowedValues,
                Detail = model.Detail
            };
        }

        public static ProcessStepHintDto ToDto(this ProcessStepHintModel model)
        {
            return new ProcessStepHintDto
            {
                Id = model.Id,
                Text = model.Text,
                AuthorUserId = model.AuthorUserId,
                AuthorName = model.AuthorName,
                CreatedAt = model.CreatedAt,
                Pinned = model.Pinned
            };
        }

        public static VisaProcessTemplateModel ToModel(this SaveVisaProcessTemplateDto dto)
        {
            return new VisaProcessTemplateModel
            {
                Name = dto.Name,
                Country = dto.Country,
                Category = dto.Category,
                Description = dto.Description,
                Status = dto.Status,
                IsDefaultForCountry = dto.IsDefaultForCountry,
                Steps = dto.Steps.Select(s => s.ToModel()).ToList()
            };
        }

        public static VisaProcessStepDefinitionModel ToModel(this VisaProcessStepDefinitionDto dto)
        {
            return new VisaProcessStepDefinitionModel
            {
                Key = dto.Key,
                Order = dto.Order,
                Label = dto.Label,
                Description = dto.Description,
                Phase = dto.Phase,
                Enabled = dto.Enabled,
                CanReopen = dto.CanReopen,
                PractitionerTagId = dto.PractitionerTagId,
                Fields = dto.Fields.Select(f => f.ToModel()).ToList(),
                RequiredEvidenceCategories = dto.RequiredEvidenceCategories,
                Preconditions = dto.Preconditions.Select(p => p.ToModel()).ToList(),
                SetsEnrolmentStatusTo = dto.SetsEnrolmentStatusTo,
                Hints = dto.Hints.Select(h => h.ToModel()).ToList()
            };
        }

        public static StepFieldDefinitionModel ToModel(this StepFieldDefinitionDto dto)
        {
            return new StepFieldDefinitionModel
            {
                FieldKey = dto.FieldKey,
                Label = dto.Label,
                InputType = dto.InputType,
                Options = dto.Options,
                IsRequired = dto.IsRequired
            };
        }

        public static StepPreconditionModel ToModel(this StepPreconditionDto dto)
        {
            return new StepPreconditionModel
            {
                Type = dto.Type,
                SourceStepKey = dto.SourceStepKey,
                FieldKey = dto.FieldKey,
                AllowedValues = dto.AllowedValues,
                Detail = dto.Detail
            };
        }

        public static ProcessStepHintModel ToModel(this ProcessStepHintDto dto)
        {
            return new ProcessStepHintModel
            {
                Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
                Text = dto.Text,
                AuthorUserId = dto.AuthorUserId,
                AuthorName = dto.AuthorName,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                Pinned = dto.Pinned
            };
        }
    }
}
