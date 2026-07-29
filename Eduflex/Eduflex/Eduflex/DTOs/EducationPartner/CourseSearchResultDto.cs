namespace Eduflex.DTOs.EducationPartner
{
    public class CourseSearchResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<string> Intakes { get; set; } = new();
        public decimal TuitionFee { get; set; }
        public string TuitionCurrency { get; set; } = string.Empty;
        public string EducationPartnerId { get; set; } = string.Empty;
        public string UniName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
