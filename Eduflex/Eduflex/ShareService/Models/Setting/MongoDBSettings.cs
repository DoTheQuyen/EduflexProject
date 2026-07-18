namespace ShareService.Models.Setting
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
    }
}