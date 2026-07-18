using DBMigration.Services.Interface;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.Models.Setting;
using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.Student;

namespace DBMigration.Services.Services
{
   

    public class DatabaseService : IDatabaseService
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<DatabaseService> _logger;
        private readonly MongoDBSettings _settings;

        // Updated constructor
        public DatabaseService(IMongoClient mongoClient, MongoDBSettings settings, ILogger<DatabaseService> logger)
        {
            _settings = settings;
            _database = mongoClient.GetDatabase(settings.DatabaseName);
            _logger = logger;
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

        public async Task CreateCollectionsAsync()
        {
            var collections = new Dictionary<string, Action<IMongoDatabase>>
            {
                ["Applications"] = db => db.GetCollection<ApplicationModel>("Applications"),
                ["Students"] = db => db.GetCollection<StudentModel>("Students"),
                ["Users"] = db => db.GetCollection<UserModel>("Users")
            };

            foreach (var (collectionName, createAction) in collections)
            {
                try
                {
                    // Check if collection exists
                    var filter = new BsonDocument("name", collectionName);
                    var options = new ListCollectionNamesOptions { Filter = filter };

                    if (!await _database.ListCollectionNames(options).AnyAsync())
                    {
                        createAction(_database);
                        _logger.LogInformation($"✅ Created collection: {collectionName}");

                        // Create indexes
                        await CreateIndexesAsync(collectionName);
                    }
                    else
                    {
                        _logger.LogInformation($"ℹ️ Collection already exists: {collectionName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating collection: {collectionName}");
                }
            }
        }

        private async Task CreateIndexesAsync(string collectionName)
        {
            switch (collectionName)
            {
                case "Applications":
                    var applications = _database.GetCollection<ApplicationModel>(collectionName);
                    await applications.Indexes.CreateManyAsync(new[]
                    {
                    new CreateIndexModel<ApplicationModel>(Builders<ApplicationModel>.IndexKeys.Ascending(a => a.StudentId)),
                    new CreateIndexModel<ApplicationModel>(Builders<ApplicationModel>.IndexKeys.Ascending(a => a.Status)),
                    new CreateIndexModel<ApplicationModel>(Builders<ApplicationModel>.IndexKeys.Descending(a => a.DateApplied))
                });
                    break;

                case "Students":
                    var students = _database.GetCollection<StudentModel>(collectionName);
                    await students.Indexes.CreateOneAsync(
                        new CreateIndexModel<StudentModel>(Builders<StudentModel>.IndexKeys.Ascending(s => s.Email),
                        new CreateIndexOptions { Unique = true }));
                    break;

                case "Users":
                    var users = _database.GetCollection<UserModel>(collectionName);
                    await users.Indexes.CreateOneAsync(
                        new CreateIndexModel<UserModel>(Builders<UserModel>.IndexKeys.Ascending(u => u.Email),
                        new CreateIndexOptions { Unique = true }));
                    break;
            }
            _logger.LogInformation($"✅ Created indexes for: {collectionName}");
        }

        public async Task DropCollectionsAsync()
        {
            var collections = new[] { "Applications", "Students", "Users" };

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

        public async Task InsertTestDataAsync()
        {
            _logger.LogInformation("📊 Inserting test data...");

            // Insert Students
            var studentsCollection = _database.GetCollection<StudentModel>("Students");
            var students = GetSampleStudents();
            if (await studentsCollection.CountDocumentsAsync(_ => true) == 0)
            {
                await studentsCollection.InsertManyAsync(students);
                _logger.LogInformation($"✅ Inserted {students.Count} students");
            }

            // Insert Users
            var usersCollection = _database.GetCollection<UserModel>("Users");
            var users = GetSampleUsers(students);
            if (await usersCollection.CountDocumentsAsync(_ => true) == 0)
            {
                await usersCollection.InsertManyAsync(users);
                _logger.LogInformation($"✅ Inserted {users.Count} users");
            }

            // Insert Applications
            var applicationsCollection = _database.GetCollection<ApplicationModel>("Applications");
            var applications = GetSampleApplications(students);
            if (await applicationsCollection.CountDocumentsAsync(_ => true) == 0)
            {
                await applicationsCollection.InsertManyAsync(applications);
                _logger.LogInformation($"✅ Inserted {applications.Count} applications");
            }
        }

        public async Task ClearTestDataAsync()
        {
            var collections = new[] { "Applications", "Students", "Users" };

            foreach (var collectionName in collections)
            {
                try
                {
                    var collection = _database.GetCollection<BsonDocument>(collectionName);
                    await collection.DeleteManyAsync(new BsonDocument());
                    _logger.LogInformation($"✅ Cleared data from: {collectionName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error clearing data from: {collectionName}");
                }
            }
        }

        private List<StudentModel> GetSampleStudents()
        {
            return new List<StudentModel>
        {
            new StudentModel
            {
                Email = "john.doe@student.edu",
                FirstName = "John",
                LastName = "Doe",
                Nationality = "American",
                DateOfBirth = new DateTime(2000, 5, 15),
                PhoneNumber = "+1234567890",
                CreatedAt = DateTime.UtcNow
            },
            new StudentModel
            {
                Email = "jane.smith@student.edu",
                FirstName = "Jane",
                LastName = "Smith",
                Nationality = "Canadian",
                DateOfBirth = new DateTime(2001, 8, 22),
                PhoneNumber = "+1987654321",
                CreatedAt = DateTime.UtcNow
            },
            new StudentModel
            {
                Email = "mike.wilson@student.edu",
                FirstName = "Mike",
                LastName = "Wilson",
                Nationality = "British",
                DateOfBirth = new DateTime(1999, 3, 10),
                PhoneNumber = "+441632960123",
                CreatedAt = DateTime.UtcNow
            }
        };
        }

        private List<UserModel> GetSampleUsers(List<StudentModel> students)
        {
            // Simple password hash (in real app, use proper hashing)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            return students.Select(s => new UserModel
            {
                Email = s.Email,
                PasswordHash = passwordHash,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Role = "Student",
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        private List<ApplicationModel> GetSampleApplications(List<StudentModel> students)
        {
            var applications = new List<ApplicationModel>();
            var now = DateTime.UtcNow;
            var random = new Random();

            var applicationTypes = new[] { "Scholarship", "Course Registration", "Housing", "Employment", "Admission" };
            var statuses = new[] { "Pending", "Approved", "Rejected" };

            foreach (var student in students)
            {
                for (int i = 1; i <= 3; i++)
                {
                    var appType = applicationTypes[random.Next(applicationTypes.Length)];
                    var status = statuses[random.Next(statuses.Length)];

                    applications.Add(new ApplicationModel
                    {
                        StudentId = student.Email,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        Description = $"{appType} Application #{i}",
                        ApplicationType = appType,
                        DateApplied = now.AddDays(-random.Next(1, 60)),
                        Status = status,
                        Details = $"This is a sample {appType.ToLower()} application for {student.FirstName} {student.LastName}.",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            return applications;
        }
    }
}
