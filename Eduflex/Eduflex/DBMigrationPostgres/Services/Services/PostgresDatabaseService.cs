using DBMigrationPostgres.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using ShareService.DataAccess.Postgres;
using ShareService.Models.Auth;
using ShareService.Models.Department;
using ShareService.Models.Role;
using System.Security.Cryptography;
using System.Text;

namespace DBMigrationPostgres.Services.Services;

public class PostgresDatabaseService : IPostgresDatabaseService
{
    private readonly EduflexPostgresContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresDatabaseService> _logger;

    public PostgresDatabaseService(EduflexPostgresContext context, IConfiguration configuration, ILogger<PostgresDatabaseService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        return await _context.Database
            .SqlQueryRaw<string>("SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename")
            .ToListAsync();
    }

    public async Task<long> GetTableRowCountAsync(string tableName)
    {
        try
        {
            // tableName is a SQL identifier (which table to name), not a data value — EF's
            // "safe" SqlQuery overload only parameterizes values, and parameterizing an
            // identifier position isn't valid SQL anyway. Safe here because tableName always
            // comes from GetTableNamesAsync(), sourced from Postgres's own pg_tables catalog —
            // never from user input.
#pragma warning disable EF1002
            var counts = await _context.Database
                .SqlQueryRaw<long>($"SELECT COUNT(*) FROM \"{tableName}\"")
                .ToListAsync();
#pragma warning restore EF1002

            return counts.FirstOrDefault();
        }
        catch
        {
            return 0;
        }
    }

    public async Task DropAllTablesAsync()
    {
        // Drops whatever actually exists — including __EFMigrationsHistory — so a subsequent
        // "Run Database Migrations" rebuilds everything from scratch. Mirrors DBMigration's
        // DropCollectionsAsync, which drops its own _migrations tracking collection too.
        var tableNames = await GetTableNamesAsync();

        foreach (var tableName in tableNames)
        {
            // Same identifier-vs-value reasoning as GetTableRowCountAsync above.
#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS \"{tableName}\" CASCADE");
#pragma warning restore EF1002
            _logger.LogInformation($"✅ Dropped table: {tableName}");
        }
    }

    // Mirrors Eduflex.Controllers.AuthController.HashPassword exactly — that's the only
    // hashing scheme the login endpoint actually verifies against (SHA-256, not BCrypt).
    private string HashPassword(string password)
    {
        var salt = _configuration["JWT:Salt"]
            ?? throw new InvalidOperationException("JWT:Salt is not configured. Add it to DBMigrationPostgres/appsettings.json.");

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    // Same 15 department-assigned Staff (3 Finance / 4 Student Consultant / 4 VISA Consultant)
    // + 5 plain Students as DBMigration's Mongo seed set, minus the Administration group and
    // Student-profile creation (no Students/Departments-Administration equivalent in Postgres
    // yet) — kept as the same names/emails so the two seed sets stay recognizable side by side.
    private class SeedPerson
    {
        public string FirstName = "";
        public string LastName = "";
        public string Department = "";
        public bool IsStudent;
        public int Index;
    }

    private static readonly List<SeedPerson> DepartmentTestPeople = new()
    {
        new() { FirstName = "Alice", LastName = "Nguyen", Department = "Finance", Index = 1 },
        new() { FirstName = "Brian", LastName = "Tran", Department = "Finance", Index = 2 },
        new() { FirstName = "Chloe", LastName = "Le", Department = "Finance", Index = 3 },
        new() { FirstName = "David", LastName = "Pham", IsStudent = true, Index = 4 },
        new() { FirstName = "Emma", LastName = "Vo", IsStudent = true, Index = 5 },

        new() { FirstName = "Frank", LastName = "Ho", Department = "Student Consultant", Index = 6 },
        new() { FirstName = "Grace", LastName = "Bui", Department = "Student Consultant", Index = 7 },
        new() { FirstName = "Henry", LastName = "Dang", Department = "Student Consultant", Index = 8 },
        new() { FirstName = "Isla", LastName = "Truong", Department = "Student Consultant", Index = 9 },
        new() { FirstName = "Jack", LastName = "Ngo", IsStudent = true, Index = 10 },

        new() { FirstName = "Kate", LastName = "Doan", Department = "VISA Consultant", Index = 11 },
        new() { FirstName = "Liam", LastName = "Mai", Department = "VISA Consultant", Index = 12 },
        new() { FirstName = "Mia", LastName = "Duong", Department = "VISA Consultant", Index = 13 },
        new() { FirstName = "Noah", LastName = "Ta", Department = "VISA Consultant", Index = 14 },
        new() { FirstName = "Olivia", LastName = "Vu", IsStudent = true, Index = 15 },
    };

    private static string EmailFor(SeedPerson p) => $"{p.FirstName.ToLowerInvariant()}.{p.LastName.ToLowerInvariant()}@eduflex.net.au";

    private async Task<DepartmentModel> GetOrCreateDepartmentAsync(string name)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
        if (department != null)
        {
            return department;
        }

        department = new DepartmentModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Description = $"{name} department (seeded test data)",
            MemberUserIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"✅ Seeded '{name}' department (was missing in this environment)");

        return department;
    }

    private async Task<RoleModel> GetOrCreateRoleAsync(string name, string description)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role != null)
        {
            return role;
        }

        role = new RoleModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Description = description,
            PermissionIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"✅ Seeded '{name}' role (was missing in this environment)");

        return role;
    }

    public async Task InsertTestDataAsync()
    {
        _logger.LogInformation("📊 Inserting department test data...");

        // Bootstrap Admin login — without this, a brand-new environment (fresh tables +
        // migrations run, but no data yet) has no way to log in and start managing anything.
        var adminEmail = "admin@eduflex.net.au";
        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existingAdmin == null)
        {
            var adminRole = await GetOrCreateRoleAsync("Admin", "Full administrative access");

            _context.Users.Add(new UserModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Email = adminEmail,
                PasswordHash = HashPassword("admin123"),
                FirstName = "Admin",
                LastName = "User",
                Mobile = "0400000000",
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation($"✅ Seeded bootstrap Admin login ({adminEmail} / admin123)");
        }

        var staffRole = await GetOrCreateRoleAsync("Staff", "Standard staff access");
        var studentRole = await GetOrCreateRoleAsync("Student", "Standard authenticated user");
        var passwordHash = HashPassword("test123");

        foreach (var person in DepartmentTestPeople)
        {
            var email = EmailFor(person);

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                continue;
            }

            var mobile = $"04{person.Index:D2}000{person.Index:D3}";

            var user = new UserModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Email = email,
                PasswordHash = passwordHash,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Mobile = mobile,
                RoleId = person.IsStudent ? studentRole.Id : staffRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (!person.IsStudent)
            {
                var department = await GetOrCreateDepartmentAsync(person.Department);
                department.MemberUserIds.Add(user.Id);
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("✅ Test data insert complete");
    }

    public async Task ClearTestDataAsync()
    {
        var emails = DepartmentTestPeople.Select(EmailFor).ToList();

        var usersToRemove = await _context.Users.Where(u => emails.Contains(u.Email)).ToListAsync();
        var userIds = usersToRemove.Select(u => u.Id).ToList();

        if (userIds.Count > 0)
        {
            var departments = await _context.Departments.ToListAsync();
            foreach (var department in departments)
            {
                department.MemberUserIds.RemoveAll(id => userIds.Contains(id));
            }
        }

        _context.Users.RemoveRange(usersToRemove);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"✅ Cleared {usersToRemove.Count} test users (only the seeded @eduflex.net.au accounts — the bootstrap Admin and everything else untouched)");
    }
}
