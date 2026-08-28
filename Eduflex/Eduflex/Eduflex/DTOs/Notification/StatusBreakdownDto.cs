namespace Eduflex.DTOs.Notification
{
    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StatusBreakdownDto
    {
        public List<StatusCountDto> Enquiry { get; set; } = new();
        public List<StatusCountDto> Application { get; set; } = new();
        public List<StatusCountDto> Enrolment { get; set; } = new();
        public List<StatusCountDto> MigrationCase { get; set; } = new();
    }
}
