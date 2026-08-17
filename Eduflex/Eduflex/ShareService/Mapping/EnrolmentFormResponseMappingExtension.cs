using System.Net;
using System.Text;
using ShareService.Enums.DynamicForms;
using ShareService.Models.DynamicForm;
using ShareService.Models.Enrolment;

namespace ShareService.Mapping
{
    public static class EnrolmentFormResponseMappingExtension
    {
        // Snapshots the template's current questions onto the new response so a later
        // template edit never retroactively changes a form the student already started
        // or finished — see docs/08-dynamic-form-module-design.md §2.1.
        public static EnrolmentFormResponseModel CreateFromTemplate(this DynamicFormTemplateModel template, string requestedByUserId, string requestedByName)
        {
            return new EnrolmentFormResponseModel
            {
                FormTemplateId = template.Id,
                FormName = template.Name,
                QuestionsSnapshot = template.Questions
                    .OrderBy(q => q.Order)
                    .Select(q => new FormQuestionModel
                    {
                        Id = q.Id,
                        Order = q.Order,
                        QuestionText = q.QuestionText,
                        HelpText = q.HelpText,
                        AnswerType = q.AnswerType,
                        Options = new List<string>(q.Options),
                        IsRequired = q.IsRequired,
                        MaxLength = q.MaxLength,
                        OptionsHorizontal = q.OptionsHorizontal
                    })
                    .ToList(),
                BoundStepKey = template.BoundStepKey,
                Status = ResponseStatus.Requesting.ToString(),
                RequestedByUserId = requestedByUserId,
                RequestedByName = requestedByName,
                RequestedAt = DateTime.UtcNow
            };
        }

        // Renders a finalized response to a standalone HTML page for IInvoicePdfService
        // to convert to PDF — same "render exactly what's on screen" approach the
        // Financial module's invoice PDF already uses, so this needs no new PDF
        // dependency. Layout mirrors the print-preview panel shown in the builder/
        // popup/Forms tab.
        public static string RenderToHtml(this EnrolmentFormResponseModel response, string studentName, string studentEmail, string studentPhone)
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><head><meta charset=\"utf-8\" /><style>");
            sb.Append(@"
                body { font-family: Arial, sans-serif; color: #1B2430; margin: 0; padding: 32px; }
                .head { text-align: center; border-bottom: 2px solid #1B2430; padding-bottom: 14px; margin-bottom: 22px; }
                .head h1 { font-family: Georgia, 'Times New Roman', serif; font-weight: 400; font-size: 22px; margin: 0; }
                .meta { display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap; font-size: 11px; color: #5C6B7A; margin-bottom: 26px; border-bottom: 1px dotted #DCE0E4; padding-bottom: 10px; }
                .q { margin-bottom: 18px; page-break-inside: avoid; }
                .q .text { font-size: 13px; font-weight: 700; margin-bottom: 6px; }
                .q .req { color: #B3413A; }
                .q .help { font-size: 11px; color: #5C6B7A; margin: -3px 0 7px; }
                .q .answer { font-size: 13px; background: #F5F6F7; border-radius: 6px; padding: 10px 12px; }
                .q .answer.empty { color: #5C6B7A; font-style: italic; }
                .foot { margin-top: 30px; padding-top: 14px; border-top: 1px solid #DCE0E4; font-size: 11px; color: #5C6B7A; display: flex; justify-content: space-between; }
            ");
            sb.Append("</style></head><body>");

            sb.Append("<div class=\"head\"><h1>").Append(WebUtility.HtmlEncode(response.FormName)).Append("</h1></div>");

            sb.Append("<div class=\"meta\"><span>Student: ").Append(WebUtility.HtmlEncode(studentName))
              .Append("</span><span>Email: ").Append(WebUtility.HtmlEncode(studentEmail))
              .Append("</span><span>Phone: ").Append(WebUtility.HtmlEncode(studentPhone)).Append("</span></div>");

            foreach (var question in response.QuestionsSnapshot.OrderBy(q => q.Order))
            {
                var answer = response.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                sb.Append("<div class=\"q\"><div class=\"text\">")
                  .Append(WebUtility.HtmlEncode(question.QuestionText));
                if (question.IsRequired)
                {
                    sb.Append(" <span class=\"req\">*</span>");
                }
                sb.Append("</div>");
                if (!string.IsNullOrWhiteSpace(question.HelpText))
                {
                    sb.Append("<div class=\"help\">").Append(WebUtility.HtmlEncode(question.HelpText)).Append("</div>");
                }
                sb.Append(RenderAnswerHtml(question, answer));
                sb.Append("</div>");
            }

            var submittedLabel = response.SubmittedAt.HasValue
                ? $"Submitted {response.SubmittedAt.Value:dd MMM yyyy HH:mm} UTC"
                : $"Generated {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC";
            sb.Append("<div class=\"foot\"><span>").Append(submittedLabel).Append("</span></div>");

            sb.Append("</body></html>");
            return sb.ToString();
        }

        // Shared by EnrolmentService (student submit) and MigrationCaseService (staff
        // submit-on-behalf-of) — both write paths validate required questions the same way,
        // this used to be duplicated as a private method on EnrolmentService alone.
        public static void ValidateRequiredAnswers(this EnrolmentFormResponseModel response, List<FormAnswerModel> answers)
        {
            var missing = response.QuestionsSnapshot
                .Where(q => q.IsRequired)
                .Where(q =>
                {
                    var answer = answers.FirstOrDefault(a => a.QuestionId == q.Id);
                    if (answer == null) return true;

                    var isSelectType = q.AnswerType == AnswerType.SingleSelect.ToString() || q.AnswerType == AnswerType.MultiSelect.ToString();
                    return isSelectType ? answer.SelectedOptions.Count == 0 : string.IsNullOrWhiteSpace(answer.TextValue);
                })
                .Select(q => q.QuestionText)
                .ToList();

            if (missing.Count > 0)
            {
                throw new ArgumentException($"Please answer the following required question(s): {string.Join("; ", missing)}");
            }
        }

        // Applied to every write path (draft save, submit, staff edit) — the client-side
        // textarea maxlength is a UX nicety, this is the actual enforcement. Shared for the
        // same reason as ValidateRequiredAnswers above.
        public static void ValidateAnswerLengths(this EnrolmentFormResponseModel response, List<FormAnswerModel> answers)
        {
            var overLimit = new List<string>();
            foreach (var question in response.QuestionsSnapshot.Where(q => q.AnswerType == AnswerType.RichText.ToString()))
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var limit = question.MaxLength ?? DynamicFormLimits.RichTextAnswerMaxLength;
                if (answer?.TextValue != null && answer.TextValue.Length > limit)
                {
                    overLimit.Add($"{question.QuestionText} (max {limit} characters)");
                }
            }

            if (overLimit.Count > 0)
            {
                throw new ArgumentException($"The following answer(s) exceed their character limit: {string.Join("; ", overLimit)}");
            }
        }

        private static string RenderAnswerHtml(FormQuestionModel question, FormAnswerModel? answer)
        {
            if (answer == null)
            {
                return "<div class=\"answer empty\">No answer provided</div>";
            }

            if (question.AnswerType == AnswerType.MultiSelect.ToString())
            {
                if (answer.SelectedOptions.Count == 0)
                {
                    return "<div class=\"answer empty\">No answer provided</div>";
                }
                return "<div class=\"answer\">" + WebUtility.HtmlEncode(string.Join(", ", answer.SelectedOptions)) + "</div>";
            }

            if (question.AnswerType == AnswerType.SingleSelect.ToString())
            {
                var selected = answer.SelectedOptions.FirstOrDefault();
                return string.IsNullOrWhiteSpace(selected)
                    ? "<div class=\"answer empty\">No answer provided</div>"
                    : "<div class=\"answer\">" + WebUtility.HtmlEncode(selected) + "</div>";
            }

            if (string.IsNullOrWhiteSpace(answer.TextValue))
            {
                return "<div class=\"answer empty\">No answer provided</div>";
            }

            // RichText answers are stored as HTML from the Quill editor — embedded as-is so
            // formatting renders, not encoded (which would show literal tags). YesNo is always
            // the plain literal "Yes"/"No", still encoded for consistency/defense in depth.
            return question.AnswerType == AnswerType.RichText.ToString()
                ? "<div class=\"answer\">" + answer.TextValue + "</div>"
                : "<div class=\"answer\">" + WebUtility.HtmlEncode(answer.TextValue).Replace("\n", "<br/>") + "</div>";
        }
    }
}
