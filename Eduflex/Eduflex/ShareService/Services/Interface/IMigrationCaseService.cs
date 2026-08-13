using ShareService.Common;
using ShareService.Models.DynamicForm;
using ShareService.Models.Enrolment;
using ShareService.Models.MigrationCase;

namespace ShareService.Services.Interface
{
    public interface IMigrationCaseService
    {
        Task<PagedResult<MigrationCaseModel>> GetCasesAsync(MigrationCaseFilter filter, string userId);
        Task<MigrationCaseModel?> GetCaseAsync(string id, string userId);

        Task<MigrationCaseModel> CreateCaseAsync(
            string templateId, string primaryContactName, string? primaryContactEmail,
            string? primaryContactMobile, string? notes, string actingUserId);

        Task<bool> UpdateCustomerInfoAsync(string id, MigrationCaseModel customerInfo, string actingUserId);

        Task<bool> SaveStepDraftAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);
        Task<bool> CompleteStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId);
        Task<bool> ReopenStepAsync(string id, string stepKey, string actingUserId);

        Task<MigrationCaseDocumentModel> AddDocumentAsync(string id, MigrationCaseDocumentModel document, string actingUserId);
        Task<bool> DeleteDocumentAsync(string id, string documentId, string actingUserId);

        Task<bool> ReassignOwnerAsync(string id, string newOwnerUserId, string actingUserId);

        Task<EnrolmentCommunicationModel> SendCommunicationAsync(
            string id, string toEmail, string recipientType, string subject, string body,
            string? templateKey, List<string> attachedDocumentIds, string actingUserId);

        Task<EnrolmentFormResponseModel> RequestFormAsync(string id, string formTemplateId, string actingUserId);
        Task<bool> SaveFormAnswersAsync(string id, string responseId, List<FormAnswerModel> answers, bool markSubmitted, string actingUserId);
        Task<bool> WithdrawFormRequestAsync(string id, string responseId, string actingUserId);
        Task<bool> ArchiveFormResponseAsync(string id, string responseId, string actingUserId);
        Task<bool> SetFormResponseStatusAsync(string id, string responseId, string newStatus, string actingUserId);
        Task<bool> ReopenFormForEditAsync(string id, string responseId, string actingUserId);
        Task<(byte[] Content, string FileName)> ExportFormAsync(string id, string responseId, string actingUserId);
    }
}
