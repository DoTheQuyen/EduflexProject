namespace Eduflex.DTOs.Student
{
    public class DuplicateCheckResultDto
    {
        public bool IsDuplicate { get; set; }
        public string? MatchedField { get; set; }
        public string? ExistingStudentId { get; set; }
        public string? ExistingUserId { get; set; }
        public bool ExistingIsActive { get; set; }
    }
}
