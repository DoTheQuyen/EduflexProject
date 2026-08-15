namespace Eduflex.DTOs.Settings
{
    public class UpdateSettingsDto
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
        public string ChatGroqApiUrl { get; set; } = string.Empty;
        public string ChatOpenRouterApiUrl { get; set; } = string.Empty;
        public string ChatGeminiModel { get; set; } = string.Empty;
        public string ChatGroqModel { get; set; } = string.Empty;
        public string ChatOpenRouterModel { get; set; } = string.Empty;
    }
}
