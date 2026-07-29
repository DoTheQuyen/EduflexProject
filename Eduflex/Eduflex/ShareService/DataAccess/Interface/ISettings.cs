using ShareService.Models.Settings;

namespace ShareService.DataAccess.Interface
{
    public interface ISettings
    {
        Task<SettingsModel?> GetSettingsAsync();
        Task<SettingsModel> UpsertSettingsAsync(SettingsModel settings);
    }
}
