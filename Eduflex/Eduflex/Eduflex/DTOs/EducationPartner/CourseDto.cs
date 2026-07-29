namespace Eduflex.DTOs.EducationPartner
{
    public class CourseDto
    {
        public string Id { get; set; } = string.Empty;
        public string EducationPartnerId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<string> Intakes { get; set; } = new();
        public List<string> StudyModes { get; set; } = new();
        public List<string> Campuses { get; set; } = new();
        public decimal TuitionFee { get; set; }
        public decimal TotalTuitionFee { get; set; }
        public string TuitionCurrency { get; set; } = string.Empty;
        public int? CourseDurationMonths { get; set; }
        public decimal CommissionBaseRate { get; set; }
    }
}
