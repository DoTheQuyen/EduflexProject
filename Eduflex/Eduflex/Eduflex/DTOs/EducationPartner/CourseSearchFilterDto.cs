namespace Eduflex.DTOs.EducationPartner
{
    public class CourseSearchFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CourseName { get; set; }
        public string? UniName { get; set; }
        public string? Country { get; set; }
        public string? Intake { get; set; }
        public string? Campus { get; set; }
        public string? StudyMode { get; set; }
        public decimal? MaxTuition { get; set; }
    }
}
