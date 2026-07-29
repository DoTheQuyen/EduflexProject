namespace Eduflex.DTOs.Settings
{
    public class DocumentUploadSettingsDto
    {
        public UploadLimitDto Default { get; set; } = new();
        public UploadLimitDto Other { get; set; } = new();
    }
}
