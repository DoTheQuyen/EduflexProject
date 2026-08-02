using Eduflex.DTOs.DynamicForm;
using ShareService.Models.Enrolment;

namespace Eduflex.Mapping.DynamicForm
{
    public static class EnrolmentFormResponseMappingExtension
    {
        public static EnrolmentFormResponseDto ToDto(this EnrolmentFormResponseModel model)
        {
            return new EnrolmentFormResponseDto
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

        public static FormAnswerDto ToDto(this FormAnswerModel model)
        {
            return new FormAnswerDto
            {
                QuestionId = model.QuestionId,
                TextValue = model.TextValue,
                SelectedOptions = model.SelectedOptions
            };
        }

        public static List<FormAnswerModel> ToModel(this SaveFormAnswersDto dto)
        {
            return dto.Answers.Select(a => new FormAnswerModel
            {
                QuestionId = a.QuestionId,
                TextValue = a.TextValue,
                SelectedOptions = a.SelectedOptions
            }).ToList();
        }
    }
}
