namespace ShareService.Models.Setting;

public class GroqSettings
{
    public string GroqApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
}
