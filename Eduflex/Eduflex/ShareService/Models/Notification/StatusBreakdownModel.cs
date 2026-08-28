namespace ShareService.Models.Notification
{
    // Status is the raw stored value (enum member name for Enquiry/Enrolment, the literal
    // status string for Application/MigrationCase) — Label is what's safe to show a user
    // (e.g. Enquiry's "MIR" -> "Information request" via its [Description] attribute).
    public class StatusCountModel
    {
        public string Status { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StatusBreakdownModel
    {
        public List<StatusCountModel> Enquiry { get; set; } = new();
        public List<StatusCountModel> Application { get; set; } = new();
        public List<StatusCountModel> Enrolment { get; set; } = new();
        public List<StatusCountModel> MigrationCase { get; set; } = new();
    }
}
