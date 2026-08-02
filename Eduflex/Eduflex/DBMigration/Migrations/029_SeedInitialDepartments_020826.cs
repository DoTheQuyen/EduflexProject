using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    // Seeds the four departments named in the org-structure design (no head/members —
    // those are assigned by an admin via the Departments screen once real staff exist).
    public class _029_SeedInitialDepartments_020826 : SafeMigrationBase
    {
        public override string MigrationId => "_029_SeedInitialDepartments_020826";
        public override string Name => "Seed initial Departments";
        public override string Description => "Seeds Finance, Student Consultant, VISA Consultant and Administration departments";

        private static readonly string[] DepartmentNames =
        {
            "Finance", "Student Consultant", "VISA Consultant", "Administration"
        };

        public override async Task Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("Departments");
            var now = DateTime.UtcNow;

            foreach (var name in DepartmentNames)
            {
                var existing = await collection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).FirstOrDefaultAsync();
                if (existing != null)
                {
                    Console.WriteLine($"✅ Department '{name}' already exists — skipping.");
                    continue;
                }

                var department = new BsonDocument
                {
                    { "name", name },
                    { "description", BsonNull.Value },
                    { "parentDepartmentId", BsonNull.Value },
                    { "headUserId", BsonNull.Value },
                    { "memberUserIds", new BsonArray() },
                    { "createdBy", BsonNull.Value },
                    { "createdAt", now },
                    { "updatedBy", BsonNull.Value },
                    { "updatedAt", now }
                };

                await collection.InsertOneAsync(department);
                Console.WriteLine($"✅ Seeded department: {name}");
            }
        }

        public override async Task Down(IMongoDatabase database)
        {
            if (!await CollectionExistsAsync(database, "Departments"))
                return;

            var collection = database.GetCollection<BsonDocument>("Departments");
            var result = await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("name", DepartmentNames));
            Console.WriteLine($"✅ Removed {result.DeletedCount} seeded departments");
        }
    }
}
