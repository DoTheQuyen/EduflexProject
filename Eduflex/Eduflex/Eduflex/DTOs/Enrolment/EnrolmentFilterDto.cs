using ShareService.Enums.Roles;

namespace Eduflex.DTOs.Enrolment
{
    public class EnrolmentFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public List<EnrolmentEnums>? Statuses { get; set; }

        /// <summary>When true, restrict results to enrolments owned by the calling staff member.</summary>
        public bool MineOnly { get; set; }
    }
}
