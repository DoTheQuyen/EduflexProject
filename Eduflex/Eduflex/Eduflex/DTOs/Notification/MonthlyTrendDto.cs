namespace Eduflex.DTOs.Notification
{
    public class MonthlyTrendPointDto
    {
        public string Month { get; set; } = string.Empty;
        public int Enquiry { get; set; }
        public int Application { get; set; }
        public int Enrolment { get; set; }
        public int MigrationCase { get; set; }
    }

    public class MonthlyTrendDto
    {
        public List<MonthlyTrendPointDto> Points { get; set; } = new();
    }
}
