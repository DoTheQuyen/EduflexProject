using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Settings;

namespace ShareService.DataAccess
{
    public class Settings : AuditableCollectionBase<SettingsModel>, ISettings
    {
        public Settings(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<SettingsModel>("Settings"), currentUser)
        {
        }

        public async Task<SettingsModel?> GetSettingsAsync()
        {
            return await Collection
                .Find(FilterDefinition<SettingsModel>.Empty)
                .FirstOrDefaultAsync();
        }

        // Singleton upsert: the DBMigration seeds the one document that will ever
        // exist, so an update always replaces that existing row by its own Id.
        public async Task<SettingsModel> UpsertSettingsAsync(SettingsModel settings)
        {
            var existing = await GetSettingsAsync();
            if (existing == null)
            {
                await InsertOneAsync(settings);
                return settings;
            }

            settings.Id = existing.Id;
            return await FindOneAndReplaceAsync(s => s.Id == existing.Id, settings);
        }
    }
}
