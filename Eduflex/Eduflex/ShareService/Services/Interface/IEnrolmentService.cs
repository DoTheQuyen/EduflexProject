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
        Task<bool> UpdateEnrolmentAsync(string id, EnrolmentModel updateModel, string actingUserId);
        Task<bool> ReassignOwnerAsync(string id, string newOwnerUserId, string actingUserId);
        Task<EnrolmentDocumentModel> AddDocumentAsync(string id, EnrolmentDocumentModel document, string actingUserId);
        Task<bool> RenameDocumentAsync(string id, string documentId, string newFileName, string actingUserId);
        Task<bool> DeleteDocumentAsync(string id, string documentId, string actingUserId);
        Task<EnrolmentCommunicationModel> SendCommunicationAsync(string id, string toEmail, string recipientType, string subject, string body, string? templateKey, List<string> attachedDocumentIds, string actingUserId);
        Task<bool> SaveVisaStepDraftAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);
        Task<bool> CompleteVisaStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);

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
        Task<bool> SaveFormDraftAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId);
        Task<bool> SubmitFormAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId);
    }
}
