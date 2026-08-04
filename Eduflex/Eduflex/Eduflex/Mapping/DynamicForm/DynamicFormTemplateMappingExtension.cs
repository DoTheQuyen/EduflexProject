using Eduflex.DTOs.DynamicForm;
using ShareService.Models.DynamicForm;

namespace Eduflex.Mapping.DynamicForm
{
    public static class DynamicFormTemplateMappingExtension
    {
        public static DynamicFormTemplateDto ToDto(this DynamicFormTemplateModel model)
        {
            return new DynamicFormTemplateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Status = model.Status,
                BoundStepKey = model.BoundStepKey,
                Questions = model.Questions.OrderBy(q => q.Order).Select(q => q.ToDto()).ToList()
            };
        }

        public static FormQuestionDto ToDto(this FormQuestionModel model)
        {
            return new FormQuestionDto
            {
                Id = model.Id,
                Order = model.Order,
                QuestionText = model.QuestionText,
                HelpText = model.HelpText,
                AnswerType = model.AnswerType,
                Options = model.Options,
                IsRequired = model.IsRequired,
                MaxLength = model.MaxLength,
                OptionsHorizontal = model.OptionsHorizontal
            };
        }

        public static DynamicFormTemplateModel ToModel(this SaveDynamicFormTemplateDto dto)
        {
            return new DynamicFormTemplateModel
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = dto.Status,
                BoundStepKey = dto.BoundStepKey,
                Questions = dto.Questions.Select(q => q.ToModel()).ToList()
            };
        }

        public static FormQuestionModel ToModel(this FormQuestionDto dto)
        {
            return new FormQuestionModel
            {
                Id = dto.Id,
                Order = dto.Order,
                QuestionText = dto.QuestionText,
                HelpText = dto.HelpText,
                AnswerType = dto.AnswerType,
                Options = dto.Options,
                IsRequired = dto.IsRequired,
                MaxLength = dto.MaxLength,
                OptionsHorizontal = dto.OptionsHorizontal
            };
        }
    }
}
