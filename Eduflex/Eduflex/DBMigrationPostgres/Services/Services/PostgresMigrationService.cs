using DBMigrationPostgres.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ShareService.DataAccess.Postgres;
using System.Data;
using System.Diagnostics;

namespace DBMigrationPostgres.Services.Services;


public class PostgresMigrationService : IPostgresMigrationService
{
    private readonly EduflexPostgresContext _context;
    private readonly ILogger<PostgresMigrationService> _logger;

    public PostgresMigrationService(EduflexPostgresContext context, ILogger<PostgresMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RunMigrationsAsync()
    {
        try
        {
            var connection = await GetOpenConnectionAsync();
            await EnsureMigrationsTableAsync(connection);

            var appliedIds = await GetAppliedMigrationIdsAsync(connection);
            var pending = DiscoverMigrations()
                .Where(m => !appliedIds.Contains(m.MigrationId))
                .OrderBy(m => m.MigrationId)
                .ToList();

            if (pending.Count == 0)
            {
                _logger.LogInformation("✅ No pending migrations found.");
                return true;
            }

            _logger.LogInformation($"📋 Found {pending.Count} pending migration(s)");

            foreach (var migration in pending)
            {
                var success = await ExecuteMigrationAsync(connection, migration);
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

    public async Task<List<string>> GetAppliedMigrationsAsync()
    {
        var connection = await GetOpenConnectionAsync();
        await EnsureMigrationsTableAsync(connection);
        return await GetAppliedMigrationIdsAsync(connection);
    }

    public async Task<List<string>> GetPendingMigrationsAsync()
    {
        var applied = await GetAppliedMigrationsAsync();
        return DiscoverMigrations()
            .Select(m => m.MigrationId)
            .Where(id => !applied.Contains(id))
            .OrderBy(id => id)
            .ToList();
    }

    private async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static async Task EnsureMigrationsTableAsync(NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS "_migrations" (
                "MigrationId" text NOT NULL PRIMARY KEY,
                "Name" text NOT NULL,
                "Description" text NOT NULL,
                "AppliedAt" timestamp with time zone NOT NULL,
                "ExecutionTimeMs" bigint NOT NULL,
                "Success" boolean NOT NULL,
                "ErrorMessage" text
            );
            """, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> GetAppliedMigrationIdsAsync(NpgsqlConnection connection)
    {
        var ids = new List<string>();

        await using var cmd = new NpgsqlCommand("""SELECT "MigrationId" FROM "_migrations" WHERE "Success" = true;""", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    // Discovers every SafePgMigrationBase-derived class whose name starts with "_" —
    // the exact same convention DBMigration uses to discover its Mongo migrations.
    private static List<IPgMigration> DiscoverMigrations()
    {
        return typeof(PostgresMigrationService).Assembly.GetTypes()
            .Where(t => typeof(IPgMigration).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract
                     && t.Name.StartsWith("_"))
            .Select(t => (IPgMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.MigrationId)
            .ToList();
    }

    private async Task<bool> ExecuteMigrationAsync(NpgsqlConnection connection, IPgMigration migration)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation($"🔄 Running migration: {migration.MigrationId} - {migration.Name}");

            await migration.Up(connection);
            stopwatch.Stop();

            await RecordMigrationAsync(connection, migration, stopwatch.ElapsedMilliseconds, true, null);

            _logger.LogInformation($"✅ Migration completed: {migration.MigrationId} ({stopwatch.ElapsedMilliseconds}ms)");
            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordMigrationAsync(connection, migration, stopwatch.ElapsedMilliseconds, false, ex.Message);

            _logger.LogError(ex, $"❌ Migration failed: {migration.MigrationId}");
            return false;
        }
    }

    private static async Task RecordMigrationAsync(NpgsqlConnection connection, IPgMigration migration, long executionTimeMs, bool success, string? errorMessage)
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO "_migrations" ("MigrationId", "Name", "Description", "AppliedAt", "ExecutionTimeMs", "Success", "ErrorMessage")
            VALUES (@migrationId, @name, @description, @appliedAt, @executionTimeMs, @success, @errorMessage)
            ON CONFLICT ("MigrationId") DO UPDATE SET
                "AppliedAt" = EXCLUDED."AppliedAt",
                "ExecutionTimeMs" = EXCLUDED."ExecutionTimeMs",
                "Success" = EXCLUDED."Success",
                "ErrorMessage" = EXCLUDED."ErrorMessage";
            """, connection);

        cmd.Parameters.AddWithValue("migrationId", migration.MigrationId);
        cmd.Parameters.AddWithValue("name", migration.Name);
        cmd.Parameters.AddWithValue("description", migration.Description);
        cmd.Parameters.AddWithValue("appliedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("executionTimeMs", executionTimeMs);
        cmd.Parameters.AddWithValue("success", success);
        cmd.Parameters.AddWithValue("errorMessage", (object?)errorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }
}
