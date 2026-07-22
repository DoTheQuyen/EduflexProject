namespace ShareService.Models.Setting
{
    public class AzureEmailSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;
    }
}