namespace ShareService.Models.Setting;

public class OpenRouterSettings
{
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "nvidia/nemotron-3.5-lightning:free";
}
