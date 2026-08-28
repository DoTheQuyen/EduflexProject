namespace ShareService.Models.Notification
{
    public class MonthlyTrendPointModel
    {
        public string Month { get; set; } = string.Empty;
        public int Enquiry { get; set; }
        public int Application { get; set; }
        public int Enrolment { get; set; }
        public int MigrationCase { get; set; }
    }

    public class MonthlyTrendModel
    {
        public List<MonthlyTrendPointModel> Points { get; set; } = new();
    }
}
