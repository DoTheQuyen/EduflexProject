namespace Eduflex.DTOs.Course
{
    public class CoursePromotionFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsFeatured { get; set; }
    }
}
