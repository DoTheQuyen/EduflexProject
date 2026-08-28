using ShareService.Common;
using ShareService.Models.Enrolment;

namespace ShareService.Services.Interface
{
    public interface IEnrolmentService
    {
        Task<EnrolmentModel> CreateFromEnquiryAsync(string enquiryId, EnrolmentModel input, string? existingStudentId, string actingUserId);
        Task<EnrolmentModel> CreateIndependentAsync(EnrolmentModel input, string? existingStudentId, string actingUserId);
        Task<PagedResult<EnrolmentModel>> GetEnrolmentsAsync(EnrolmentFilter filter, string userId);
        Task<EnrolmentModel?> GetEnrolmentAsync(string id, string userId);
        Task<List<EnrolmentModel>> GetEnrolmentsForStudentAsync(string studentUserId, string actingUserId);
        Task<bool> UpdateEnrolmentAsync(string id, EnrolmentModel updateModel, string actingUserId);
        Task<bool> ReassignOwnerAsync(string id, string newOwnerUserId, string actingUserId);
        Task<EnrolmentDocumentModel> AddDocumentAsync(string id, EnrolmentDocumentModel document, string actingUserId);
        Task<bool> RenameDocumentAsync(string id, string documentId, string newFileName, string? note, string actingUserId);
        Task<bool> DeleteDocumentAsync(string id, string documentId, string actingUserId);
        Task<EnrolmentCommunicationModel> SendCommunicationAsync(string id, string toEmail, string recipientType, string subject, string body, string? templateKey, List<string> attachedDocumentIds, string actingUserId);
        Task<bool> SaveVisaStepDraftAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);
        Task<bool> CompleteVisaStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);
        Task<bool> ReopenVisaStepAsync(string id, string stepKey, string actingUserId);

        // Separate from CompleteVisaStepAsync's VisaOutcome branch — recording the outcome
        // and financially closing out the enrolment are two different staff decisions.
        // Only creates the Financial record when the outcome was Granted.
        Task<bool> FinalizeEnrolmentAsync(string id, string actingUserId);

        // Merges (not replaces) the given keys into one step's Fields bag — used by
        // InvoiceService to record invoiceId/invoiceSentAt/invoicePaidAt without clobbering
        // whatever else staff have already saved on that step (e.g. a comment).
        Task SetStepFieldsAsync(string id, string stepKey, Dictionary<string, string> fieldsToMerge, string auditDescription, string actingUserId);

        // ----- Course Applications (Enrolment Form step's nested sub-panels) -----
        Task<bool> SetMaxApplicationsOverrideAsync(string id, int newMax, string actingUserId);
        Task<CourseApplicationModel> AddCourseApplicationAsync(string id, string educationPartnerId, string courseId, string actingUserId);
        Task<bool> UpdateCourseApplicationDetailsAsync(string id, string courseApplicationId, CourseApplicationDetailsModel details, string actingUserId);
        Task<bool> SetCourseApplicationStatusAsync(string id, string courseApplicationId, string newStatus, string actingUserId);
        Task<bool> FinalizeCourseApplicationAsync(string id, string courseApplicationId, string actingUserId);

        // ----- Dynamic Forms — staff actions (permission + ownership, via GetOwnedEnrolmentAsync) -----
        Task<EnrolmentFormResponseModel> RequestFormAsync(string id, string formTemplateId, string actingUserId);
        Task<bool> WithdrawFormRequestAsync(string id, string responseId, string actingUserId);
        Task<bool> ArchiveFormResponseAsync(string id, string responseId, string actingUserId);
        Task<bool> SetFormResponseStatusAsync(string id, string responseId, string newStatus, string actingUserId);
        Task<bool> ReopenFormForEditAsync(string id, string responseId, string actingUserId);
        Task<bool> StaffEditFormResponseAsync(string id, string responseId, List<FormAnswerModel> answers, string actingUserId);
        Task<(byte[] Content, string FileName)> ExportFormAsync(string id, string responseId, string actingUserId);

        // ----- Dynamic Forms — student actions (ownership via StudentUserId, no permission key) -----
        Task<EnrolmentModel?> GetEnrolmentForStudentByApplicationIdAsync(string applicationId, string studentUserId);
        Task<MyEnrolmentSummaryModel?> GetMyEnrolmentSummaryAsync(string applicationId, string studentUserId);
        Task<bool> SaveFormDraftAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId);
        Task<bool> SubmitFormAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId);

        Task<Dictionary<string, int>> GetMonthlyCountsAsync(string userId, DateTime since);
        Task<Dictionary<string, int>> GetStatusCountsAsync(string userId);
    }
}
