namespace Eduflex.API.DTOs
{
    public class CoursePromotionDto
    {
        public string Id { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string ScholarshipLabel { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Tuition { get; set; } = string.Empty;
        public string Opportunities { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
