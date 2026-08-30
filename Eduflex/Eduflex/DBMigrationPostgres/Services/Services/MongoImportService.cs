using DBMigrationPostgres.Models;
using DBMigrationPostgres.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ShareService.DataAccess.Postgres;
using ShareService.Models.Auth;
using ShareService.Models.Role;

namespace DBMigrationPostgres.Services.Services;

// Phase 1 of the Mongo -> Postgres cutover: copy the existing Users/Roles data across and
// verify it, without changing anything the live app reads. Nothing here touches the Mongo
// side except reading — the source database is left completely untouched.
public class MongoImportService : IMongoImportService
{
    private readonly EduflexPostgresContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MongoImportService> _logger;

    public MongoImportService(EduflexPostgresContext context, IConfiguration configuration, ILogger<MongoImportService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> IsMongoConfiguredAsync()
    {
        var connectionString = _configuration["MongoSource:ConnectionString"];
        var databaseName = _configuration["MongoSource:DatabaseName"];
        return Task.FromResult(!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(databaseName));
    }

    private IMongoDatabase GetMongoDatabase()
    {
        var connectionString = _configuration["MongoSource:ConnectionString"]
            ?? throw new InvalidOperationException("MongoSource:ConnectionString is not configured. Add it to DBMigrationPostgres/appsettings.local.json.");
        var databaseName = _configuration["MongoSource:DatabaseName"]
            ?? throw new InvalidOperationException("MongoSource:DatabaseName is not configured. Add it to DBMigrationPostgres/appsettings.local.json.");

        return new MongoClient(connectionString).GetDatabase(databaseName);
    }

    // Npgsql rejects a DateTime whose Kind is Unspecified when writing to
    // "timestamp with time zone". Mongo stores UTC but the driver can hand back
    // Unspecified/Local depending on how the value was originally written, so every
    // timestamp gets normalized to UTC before it crosses into Postgres.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;

    public async Task<bool> ImportRolesAndUsersAsync()
    {
        var mongoDatabase = GetMongoDatabase();

        var mongoRoles = await mongoDatabase.GetCollection<RoleModel>("Roles").Find(_ => true).ToListAsync();
        var mongoUsers = await mongoDatabase.GetCollection<UserModel>("Users").Find(_ => true).ToListAsync();

        _logger.LogInformation($"📥 Read {mongoRoles.Count} role(s) and {mongoUsers.Count} user(s) from MongoDB");

        var existingRoleIds = await _context.Roles.Select(r => r.Id).ToListAsync();
        var existingUserIds = await _context.Users.Select(u => u.Id).ToListAsync();

        // Roles and Users are written in one transaction — Users.RoleId has a real FK to
        // Roles.Id, so a partial import that landed Users without their Roles would either
        // fail or leave the database in a state the FK was specifically added to prevent.
        // This atomicity is exactly what the Mongo side could never give us.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var rolesToInsert = mongoRoles.Where(r => !existingRoleIds.Contains(r.Id)).ToList();
            foreach (var role in rolesToInsert)
            {
                role.CreatedAt = AsUtc(role.CreatedAt);
                role.UpdatedAt = AsUtc(role.UpdatedAt);
                role.PermissionIds ??= new List<string>();
                _context.Roles.Add(role);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"✅ Inserted {rolesToInsert.Count} role(s) ({mongoRoles.Count - rolesToInsert.Count} already existed)");

            // Every RoleId a User points at must exist in Postgres or the FK insert fails.
            // Orphans are skipped and reported rather than aborting the whole import — a
            // user pointing at a deleted role is a pre-existing Mongo data issue, not
            // something this import should silently invent a role for.
            var validRoleIds = await _context.Roles.Select(r => r.Id).ToListAsync();

            var usersToInsert = new List<UserModel>();
            var orphanedUsers = new List<UserModel>();

            foreach (var user in mongoUsers.Where(u => !existingUserIds.Contains(u.Id)))
            {
                if (string.IsNullOrEmpty(user.RoleId) || !validRoleIds.Contains(user.RoleId))
                {
                    orphanedUsers.Add(user);
                    continue;
                }

                user.CreatedAt = AsUtc(user.CreatedAt);
                user.UpdatedAt = AsUtc(user.UpdatedAt);
                user.LastLogin = AsUtc(user.LastLogin);
                user.ResetTokenExpiry = AsUtc(user.ResetTokenExpiry);
                usersToInsert.Add(user);
            }

            _context.Users.AddRange(usersToInsert);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation($"✅ Inserted {usersToInsert.Count} user(s) ({mongoUsers.Count - usersToInsert.Count - orphanedUsers.Count} already existed)");

            foreach (var orphan in orphanedUsers)
            {
                _logger.LogWarning($"⚠️ Skipped user '{orphan.Email}' (Id {orphan.Id}) — RoleId '{orphan.RoleId}' does not exist in Roles");
            }

            if (orphanedUsers.Count > 0)
            {
                _logger.LogWarning($"⚠️ {orphanedUsers.Count} user(s) skipped due to missing roles — fix these in Mongo, then re-run the import to pick them up");
            }

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "❌ Import failed — transaction rolled back, Postgres left unchanged");
            return false;
        }
    }

    public async Task<List<ImportComparisonRow>> CompareAsync()
    {
        var mongoDatabase = GetMongoDatabase();

        var mongoRoles = await mongoDatabase.GetCollection<RoleModel>("Roles").Find(_ => true).ToListAsync();
        var mongoUsers = await mongoDatabase.GetCollection<UserModel>("Users").Find(_ => true).ToListAsync();

        var pgRoles = await _context.Roles.AsNoTracking().ToListAsync();
        var pgUsers = await _context.Users.AsNoTracking().ToListAsync();

        var pgRolesById = pgRoles.ToDictionary(r => r.Id);
        var pgUsersById = pgUsers.ToDictionary(u => u.Id);

        var roleRow = new ImportComparisonRow
        {
            EntityName = "Roles",
            MongoCount = mongoRoles.Count,
            PostgresCount = pgRoles.Count
        };

        foreach (var mongoRole in mongoRoles)
        {
            if (!pgRolesById.TryGetValue(mongoRole.Id, out var pgRole))
            {
                roleRow.MissingInPostgres++;
                continue;
            }

            if (mongoRole.Name != pgRole.Name
                || mongoRole.RoleType != pgRole.RoleType
                || !(mongoRole.PermissionIds ?? new List<string>()).OrderBy(id => id)
                    .SequenceEqual((pgRole.PermissionIds ?? new List<string>()).OrderBy(id => id)))
            {
                roleRow.FieldMismatches++;
                _logger.LogWarning($"⚠️ Role '{mongoRole.Name}' (Id {mongoRole.Id}) differs between Mongo and Postgres");
            }
        }

        var userRow = new ImportComparisonRow
        {
            EntityName = "Users",
            MongoCount = mongoUsers.Count,
            PostgresCount = pgUsers.Count
        };

        foreach (var mongoUser in mongoUsers)
        {
            if (!pgUsersById.TryGetValue(mongoUser.Id, out var pgUser))
            {
                userRow.MissingInPostgres++;
                continue;
            }

            // PasswordHash is included deliberately — if it didn't survive the copy intact,
            // every migrated user would silently be unable to log in.
            if (mongoUser.Email != pgUser.Email
                || mongoUser.PasswordHash != pgUser.PasswordHash
                || mongoUser.Mobile != pgUser.Mobile
                || mongoUser.RoleId != pgUser.RoleId
                || mongoUser.IsActive != pgUser.IsActive)
            {
                userRow.FieldMismatches++;
                _logger.LogWarning($"⚠️ User '{mongoUser.Email}' (Id {mongoUser.Id}) differs between Mongo and Postgres");
            }
        }

        return new List<ImportComparisonRow> { roleRow, userRow };
    }
}
