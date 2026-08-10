namespace Eduflex.DTOs.Settings
{
    public class SettingsDto
    {
        public int FeedbackDefaultLatestCount { get; set; }
        public int CoursePromotionDefaultLatestCount { get; set; }
        public int MaxApplicationsPerStudent { get; set; }
        public DocumentUploadSettingsDto DocumentUpload { get; set; } = new();
        public UploadLimitDto ImageUpload { get; set; } = new();
        public UploadLimitDto ContractUpload { get; set; } = new();
        public UploadLimitDto EnrolmentUpload { get; set; } = new();
        public string ChatSystemPrompt { get; set; } = string.Empty;
        public string ChatApiUrl { get; set; } = string.Empty;
    }
}
