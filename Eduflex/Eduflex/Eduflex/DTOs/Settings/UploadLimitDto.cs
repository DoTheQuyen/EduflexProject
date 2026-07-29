namespace Eduflex.DTOs.Settings
{
    public class UploadLimitDto
    {
        public double MaxSizeMB { get; set; }
        public List<string> AllowedExtensions { get; set; } = new();
        public int MaxFileCount { get; set; }
    }
}
