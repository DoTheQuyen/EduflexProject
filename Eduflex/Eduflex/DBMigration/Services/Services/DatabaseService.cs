using DBMigration.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ShareService.Models.Setting;
using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.Student;
using ShareService.Models.Address;
using System.Security.Cryptography;
using System.Text;

namespace DBMigration.Services.Services
{


    public class DatabaseService : IDatabaseService
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<DatabaseService> _logger;
        private readonly MongoDBSettings _settings;
        private readonly IConfiguration _configuration;

        // Updated constructor
        public DatabaseService(IMongoClient mongoClient, MongoDBSettings settings, ILogger<DatabaseService> logger, IConfiguration configuration)
        {
            _settings = settings;
            _database = mongoClient.GetDatabase(settings.DatabaseName);
            _logger = logger;
            _configuration = configuration;
        }

        // Mirrors Eduflex.Controllers.AuthController.HashPassword exactly — that's the only
        // hashing scheme the login endpoint actually verifies against (SHA-256, not BCrypt).
        private string HashPassword(string password)
        {
            var salt = _configuration["JWT:Salt"]
                ?? throw new InvalidOperationException("JWT:Salt is not configured. Add it to DBMigration/appsettings.json.");

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                _logger.LogInformation("✅ MongoDB connection successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ MongoDB connection failed");
                return false;
            }
        }

        public string GetOutputDirectory()
        {
            return _settings.OutputDirectory;
        }

        public async Task<BsonDocument> GetCollection(string collectionName)
        {
            
            return await _database.GetCollection<BsonDocument>(collectionName).Find(new BsonDocument()).FirstOrDefaultAsync();

        }

        public async Task DropCollectionsAsync()
        {
            // Drops whatever actually exists (including _migrations and anything added by
            // migrations, e.g. Enquiries/Feedbacks/CoursePromotions) instead of a fixed list —
            // a hardcoded subset silently goes stale every time a new collection is introduced.
            var collections = await _database.ListCollectionNames().ToListAsync();

            foreach (var collectionName in collections)
            {
                try
                {
                    await _database.DropCollectionAsync(collectionName);
                    _logger.LogInformation($"✅ Dropped collection: {collectionName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error dropping collection: {collectionName}");
                }
            }
        }

        public async Task<List<string>> GetCollectionNamesAsync()
        {
            return await _database.ListCollectionNames().ToListAsync();
        }

        public async Task<long> GetCollectionCountAsync(string collectionName)
        {
            try
            {
                var collection = _database.GetCollection<BsonDocument>(collectionName);
                return await collection.CountDocumentsAsync(new BsonDocument());
            }
            catch
            {
                return 0;
            }
        }

        // 13 department-assigned Staff (3 Finance / 4 Student Consultant / 4 VISA Consultant /
        // 2 Administration, seeded by _029_SeedInitialDepartments_020826) + 7 plain Students
        // (Student role, not added to any department — Departments is internal staff org
        // structure, students aren't part of it). Every one of the 20 still gets both a User
        // account and a matching Student profile under eduflex.net.au, reusing "test123".
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
            new() { FirstName = "Olivia", LastName = "Huynh", IsStudent = true, Index = 15 },

            new() { FirstName = "Peter", LastName = "Lam", Department = "Administration", Index = 16 },
            new() { FirstName = "Quinn", LastName = "Vu", Department = "Administration", Index = 17 },
            new() { FirstName = "Ruby", LastName = "Cao", IsStudent = true, Index = 18 },
            new() { FirstName = "Sam", LastName = "Phan", IsStudent = true, Index = 19 },
            new() { FirstName = "Tina", LastName = "Luu", IsStudent = true, Index = 20 },
        };

        private static string EmailFor(SeedPerson p) => $"{p.FirstName.ToLowerInvariant()}.{p.LastName.ToLowerInvariant()}@eduflex.net.au";

        // Reference content that's curated by hand in Local (course promotions, feedback,
        // the dynamic form template, partners, courses) rather than generated — exported to
        // JSON once from Local, then imported into any other environment as part of the same
        // "Insert Test Data" action that seeds the department Users/Students.
        private static readonly string[] ReferenceCollections =
        {
            "CoursePromotions", "Feedbacks", "DynamicFormTemplates", "EducationPartners", "Courses", "BusinessPartners"
        };

        // Project-root-relative (not bin-output-relative), same convention as
        // MongoConnectionStore.GetProjectLocalSettingsPath — survives `dotnet clean` and the
        // exported JSON lives in source control, not a throwaway build folder.
        private static string GetSeedDataDirectory()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));
            return Path.Combine(projectRoot, "SeedData");
        }

        // Run this against Local to snapshot the hand-curated reference collections into
        // DBMigration/SeedData/*.json. Preserves original _id values so cross-collection
        // references (e.g. Courses.educationPartnerId) still point at the right document
        // after being imported into another environment.
        public async Task ExportReferenceDataAsync()
        {
            var seedDir = GetSeedDataDirectory();
            Directory.CreateDirectory(seedDir);

            foreach (var collectionName in ReferenceCollections)
            {
                var collection = _database.GetCollection<BsonDocument>(collectionName);
                var documents = await collection.Find(new BsonDocument()).ToListAsync();

                var array = new BsonArray(documents);
                var json = array.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson, Indent = true });

                var filePath = Path.Combine(seedDir, $"{collectionName}.json");
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation($"✅ Exported {documents.Count} documents from {collectionName} → {filePath}");
            }
        }

        // Imports whatever ExportReferenceDataAsync last wrote. Skipped per-document by _id,
        // so re-running (or running InsertTestDataAsync repeatedly) is safe.
        private async Task ImportReferenceDataAsync()
        {
            var seedDir = GetSeedDataDirectory();

            foreach (var collectionName in ReferenceCollections)
            {
                var filePath = Path.Combine(seedDir, $"{collectionName}.json");
                if (!File.Exists(filePath))
                {
                    _logger.LogInformation($"ℹ️ No seed file for {collectionName} (run Export Reference Data against Local first). Skipping.");
                    continue;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var documents = BsonSerializer.Deserialize<BsonArray>(json).Select(v => v.AsBsonDocument).ToList();
                if (documents.Count == 0)
                {
                    continue;
                }

                var collection = _database.GetCollection<BsonDocument>(collectionName);
                var insertedCount = 0;

                foreach (var doc in documents)
                {
                    var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"])).FirstOrDefaultAsync();
                    if (existing != null)
                    {
                        continue;
                    }

                    await collection.InsertOneAsync(doc);
                    insertedCount++;
                }

                _logger.LogInformation($"✅ Imported {insertedCount} new documents into {collectionName} ({documents.Count - insertedCount} already existed)");
            }
        }

        public async Task InsertTestDataAsync()
        {
            _logger.LogInformation("📊 Inserting department test data...");

            var departmentsCollection = _database.GetCollection<BsonDocument>("Departments");
            var usersCollection = _database.GetCollection<UserModel>("Users");
            var studentsCollection = _database.GetCollection<StudentModel>("Students");

            var departmentNames = DepartmentTestPeople.Where(p => !p.IsStudent).Select(p => p.Department).Distinct().ToList();
            var departmentsByName = new Dictionary<string, BsonDocument>();
            foreach (var name in departmentNames)
            {
                var dept = await departmentsCollection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).FirstOrDefaultAsync();
                if (dept == null)
                {
                    _logger.LogWarning($"⚠️ Department '{name}' not found — run Database Migrations first (seeds Departments). Skipping test data insert.");
                    return;
                }
                departmentsByName[name] = dept;
            }

            // Bootstrap Admin login — the only account InsertTestDataAsync creates with the
            // Admin role. Without this, a brand-new environment (fresh collections + migrations
            // run, but no data yet) has no way to log in and start managing anything, since the
            // 20 department accounts below are all Staff role.
            var adminEmail = "admin@eduflex.net.au";
            var existingAdmin = await usersCollection.Find(u => u.Email == adminEmail).FirstOrDefaultAsync();
            if (existingAdmin == null)
            {
                var adminRoleId = await GetOrCreateRoleIdAsync("Admin", "Full administrative access", Array.Empty<string>());
                await usersCollection.InsertOneAsync(new UserModel
                {
                    Email = adminEmail,
                    PasswordHash = HashPassword("admin123"),
                    FirstName = "Admin",
                    LastName = "User",
                    Mobile = "0400000000",
                    RoleId = adminRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                _logger.LogInformation($"✅ Seeded bootstrap Admin login ({adminEmail} / admin123)");
            }

            var staffRoleId = await GetOrCreateRoleIdAsync("Staff", "Standard staff access", Array.Empty<string>());
            var studentRoleId = await GetOrCreateRoleIdAsync("Student", "Standard authenticated user", Array.Empty<string>());
            var passwordHash = HashPassword("test123");
            var now = DateTime.UtcNow;
            var createdCount = 0;

            foreach (var person in DepartmentTestPeople)
            {
                var email = EmailFor(person);

                var existingUser = await usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    continue;
                }

                var mobile = $"04{person.Index:D2}000{person.Index:D3}";

                var user = new UserModel
                {
                    Email = email,
                    PasswordHash = passwordHash,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    Mobile = mobile,
                    RoleId = person.IsStudent ? studentRoleId : staffRoleId,
                    IsActive = true,
                    CreatedAt = now
                };
                await usersCollection.InsertOneAsync(user);

                if (person.IsStudent)
                {
                    var student = new StudentModel
                    {
                        UserId = user.Id,
                        Email = email,
                        FirstName = person.FirstName,
                        LastName = person.LastName,
                        Nationality = "Vietnamese",
                        PassportNumber = $"PA{1000000 + person.Index}",
                        DateOfBirth = new DateTime(1990, 1, 1).AddYears(person.Index % 15).AddMonths(person.Index % 12),
                        PhoneNumber = mobile,
                        Address = new AddressModel
                        {
                            Street = $"{100 + person.Index} Collins Street",
                            City = "Melbourne",
                            State = "VIC",
                            Country = "Australia",
                            PostalCode = "3000"
                        },
                        CreatedAt = now
                    };
                    await studentsCollection.InsertOneAsync(student);
                }
                else
                {
                    await departmentsCollection.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", departmentsByName[person.Department]["_id"]),
                        Builders<BsonDocument>.Update.AddToSet("memberUserIds", user.Id));
                }

                createdCount++;
            }

            var staffCount = DepartmentTestPeople.Count(p => !p.IsStudent);
            var studentCount = DepartmentTestPeople.Count(p => p.IsStudent);
            _logger.LogInformation($"✅ Inserted {createdCount} new test User/Student pairs ({staffCount} Staff across departments, {studentCount} Student-role) — {DepartmentTestPeople.Count - createdCount} already existed. Login password for all: test123");

            await ImportReferenceDataAsync();
        }

        // Only removes the specific seeded @eduflex.net.au test accounts (and pulls them back out
        // of Departments.memberUserIds) — deliberately does NOT wipe Users/Students/Applications
        // wholesale like the old version did, since those collections now hold real accounts
        // (Admin logins, Departments/Roles-linked staff) that a blanket clear would destroy.
        public async Task ClearTestDataAsync()
        {
            var departmentsCollection = _database.GetCollection<BsonDocument>("Departments");
            var usersCollection = _database.GetCollection<UserModel>("Users");
            var studentsCollection = _database.GetCollection<StudentModel>("Students");

            var emails = DepartmentTestPeople.Select(EmailFor).ToList();

            var usersToRemove = await usersCollection.Find(u => emails.Contains(u.Email)).ToListAsync();
            var userIds = usersToRemove.Select(u => u.Id).ToList();

            if (userIds.Any())
            {
                await departmentsCollection.UpdateManyAsync(
                    new BsonDocument(),
                    Builders<BsonDocument>.Update.PullAll("memberUserIds", userIds));
            }

            var studentsResult = await studentsCollection.DeleteManyAsync(s => emails.Contains(s.Email));
            var usersResult = await usersCollection.DeleteManyAsync(u => emails.Contains(u.Email));

            _logger.LogInformation($"✅ Cleared {usersResult.DeletedCount} test users and {studentsResult.DeletedCount} test students (only the seeded @eduflex.net.au accounts — everything else untouched)");
        }

        // Mirrors the Admin/Student/Staff roles pattern from migration _010_AddRolesAndUserRoleId_200726,
        // so seed users get a valid roleId whether or not that migration has already run.
        private async Task<string> GetOrCreateRoleIdAsync(string name, string description, string[] permissions)
        {
            var rolesCollection = _database.GetCollection<BsonDocument>("Roles");
            var role = await rolesCollection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).FirstOrDefaultAsync();

            if (role == null)
            {
                role = new BsonDocument
                {
                    { "name", name },
                    { "description", description },
                    { "permissionIds", new BsonArray(permissions) }
                };
                await rolesCollection.InsertOneAsync(role);
                _logger.LogInformation($"✅ Seeded {name} role (was missing in this environment)");
            }

            return role["_id"].AsObjectId.ToString();
        }
    }
}
