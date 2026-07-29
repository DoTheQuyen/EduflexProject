using System;

namespace Eduflex.DTOs.Student
{
    public class StudentAuditEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }
}
