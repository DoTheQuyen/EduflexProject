using ShareService.Models;
using DBMigration.Models;
using DBMigration.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Services.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MigrationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMigrationRegistrationService _migrationRegistrationService;

        public MigrationService(
            IMongoDatabase database,
            ILogger<MigrationService> logger,
            IServiceProvider serviceProvider,
            IMigrationRegistrationService migrationRegistrationService)
        {
            _database = database;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _migrationRegistrationService = migrationRegistrationService;
        }

        public async Task<bool> RunMigrationsAsync()
        {
            try
            {
                // Ensure migrations collection exists
                await EnsureMigrationsCollectionAsync();

                var appliedMigrations = await GetAppliedMigrationIdsAsync();
                var allMigrations = DiscoverMigrations();

                var pendingMigrations = allMigrations
                    .Where(m => !appliedMigrations.Contains(m.MigrationId))
                    .OrderBy(m => m.MigrationId)
                    .ToList();

                if (!pendingMigrations.Any())
                {
                    _logger.LogInformation("✅ No pending migrations found.");
                    return true;
                }

                _logger.LogInformation($"📋 Found {pendingMigrations.Count} pending migrations");

                foreach (var migration in pendingMigrations)
                {
                    var success = await ExecuteMigrationAsync(migration);
                    if (!success)
                    {
                        _logger.LogError($"❌ Migration {migration.MigrationId} failed. Stopping migration process.");
                        return false;
                    }
                }

                _logger.LogInformation("✅ All migrations completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error running migrations");
                return false;
            }
        }

        public async Task<List<MigrationRecord>> GetMigrationHistoryAsync()
        {
            var collection = _database.GetCollection<MigrationRecord>("_migrations");
            return await collection.Find(_ => true)
                .SortByDescending(m => m.AppliedAt)
                .ToListAsync();
        }

        public async Task RollbackMigrationAsync(string migrationId)
        {
            var migration = DiscoverMigrations().FirstOrDefault(m => m.MigrationId == migrationId);
            if (migration == null)
            {
                throw new ArgumentException($"Migration {migrationId} not found");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await migration.Down(_database);
                stopwatch.Stop();

                var collection = _database.GetCollection<MigrationRecord>("_migrations");
                await collection.DeleteOneAsync(m => m.MigrationId == migrationId);

                _logger.LogInformation($"✅ Rolled back migration: {migrationId} ({stopwatch.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"❌ Failed to rollback migration: {migrationId}");
                throw;
            }
        }

        private async Task EnsureMigrationsCollectionAsync()
        {
            var collectionNames = await _database.ListCollectionNames().ToListAsync();
            if (!collectionNames.Contains("_migrations"))
            {
                await _database.CreateCollectionAsync("_migrations");

                var collection = _database.GetCollection<MigrationRecord>("_migrations");
                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<MigrationRecord>(Builders<MigrationRecord>.IndexKeys.Ascending(m => m.MigrationId),
                    new CreateIndexOptions { Unique = true }));

                _logger.LogInformation("✅ Created migrations tracking collection");
            }
        }

        private async Task<List<string>> GetAppliedMigrationIdsAsync()
        {
            var collection = _database.GetCollection<MigrationRecord>("_migrations");
            var records = await collection.Find(_ => true).ToListAsync();
            return records.Select(r => r.MigrationId).ToList();
        }

        private List<IMigration> DiscoverMigrations()
        {
            var migrations = new List<IMigration>();
            var migrationTypes = _migrationRegistrationService.GetMigrationTypes();

            foreach (var type in migrationTypes)
            {
                try
                {
                    var migration = (IMigration)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                    migrations.Add(migration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Failed to create instance of migration: {type.Name}");
                }
            }

            return migrations.OrderBy(m => m.MigrationId).ToList();
        }

        private async Task<bool> ExecuteMigrationAsync(IMigration migration)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var record = new MigrationRecord
            {
                MigrationId = migration.MigrationId,
                Name = migration.Name,
                Description = migration.Description,
                AppliedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation($"🔄 Running migration: {migration.MigrationId} - {migration.Name}");

                await migration.Up(_database);
                stopwatch.Stop();

                record.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                record.Success = true;

                var collection = _database.GetCollection<MigrationRecord>("_migrations");
                await collection.InsertOneAsync(record);

                _logger.LogInformation($"✅ Migration completed: {migration.MigrationId} ({stopwatch.ElapsedMilliseconds}ms)");
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                record.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                record.Success = false;
                record.ErrorMessage = ex.Message;

                var collection = _database.GetCollection<MigrationRecord>("_migrations");
                await collection.InsertOneAsync(record);

                _logger.LogError(ex, $"❌ Migration failed: {migration.MigrationId}");
                return false;
            }
        }

       
    }
}