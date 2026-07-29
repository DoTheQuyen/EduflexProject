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
    }
}
