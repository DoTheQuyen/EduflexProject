namespace Eduflex.DTOs.Settings
{
    public class UpdateSettingsDto
    {
        public int FeedbackDefaultLatestCount { get; set; }
        public int CoursePromotionDefaultLatestCount { get; set; }
        public DocumentUploadSettingsDto DocumentUpload { get; set; } = new();
        public UploadLimitDto ImageUpload { get; set; } = new();
        public UploadLimitDto ContractUpload { get; set; } = new();
        public UploadLimitDto EnrolmentUpload { get; set; } = new();
    }
}
