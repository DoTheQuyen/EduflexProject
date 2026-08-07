namespace Eduflex.DTOs.Enrolment
{
    public class EnrolmentDocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? CourseApplicationId { get; set; }
        public string? Note { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string? UploadedByUserId { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public bool IsFromStudent { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class AddEnrolmentDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? CourseApplicationId { get; set; }
        public string? Note { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
    }

    public class RenameEnrolmentDocumentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
