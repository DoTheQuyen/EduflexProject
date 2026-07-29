using ShareService.Models.Settings;

namespace ShareService.Services.Interface
{
    public interface ISettingsService
    {
        Task<SettingsModel> GetSettingsAsync();
        Task<SettingsModel> UpdateSettingsAsync(SettingsModel settings, string userId);
    }
}
