using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _004_AddUserForeignKeyToStudents_290925 : SafeMigrationBase
    {
        public override string MigrationId => "_004_AddUserForeignKeyToStudents_290925";
        public override string Name => "Add User Foreign Key to Students Table";
        public override string Description => "Update Students collection to reference Users _id as foreign key and ensure data consistency";

        public override async Task Up(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Starting migration: Add User Foreign Key to Students");

            // Check if collections exist
            if (!await CollectionExistsAsync(database, "Users"))
            {
                Console.WriteLine("❌ Users collection doesn't exist. Please create Users collection first.");
                return;
            }

            if (!await CollectionExistsAsync(database, "Students"))
            {
                Console.WriteLine("❌ Students collection doesn't exist. Please create Students collection first.");
                return;
            }

            var usersCollection = database.GetCollection<BsonDocument>("Users");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            // Step 1: Get all existing users
            var users = await usersCollection.Find(new BsonDocument()).ToListAsync();
            if (!users.Any())
            {
                Console.WriteLine("⚠️ No users found in Users collection. Please insert user data first.");
                return;
            }

            Console.WriteLine($"📊 Found {users.Count} users in the database");

            // Step 2: Update students to link with users
            var students = await studentsCollection.Find(new BsonDocument()).ToListAsync();
            Console.WriteLine($"📊 Found {students.Count} students to update");

            int updatedCount = 0;
            int createdUsersCount = 0;

            foreach (var student in students)
            {
                try
                {
                    var studentEmail = student.GetValue("email", "").ToString();

                    // Find matching user by email
                    var user = users.FirstOrDefault(u =>
                        u.GetValue("email", "").ToString().Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

                    if (user != null)
                    {
                        // Update student with userId
                        var userId = user["_id"].ToString();
                        var update = Builders<BsonDocument>.Update.Set("userId", userId);

                        await studentsCollection.UpdateOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", student["_id"]),
                            update
                        );

                        updatedCount++;
                    }
                    else
                    {
                        // Create a new user for this student if no matching user found
                        var newUserId = await CreateUserFromStudent(database, student);
                        if (!string.IsNullOrEmpty(newUserId))
                        {
                            var update = Builders<BsonDocument>.Update.Set("userId", newUserId);
                            await studentsCollection.UpdateOneAsync(
                                Builders<BsonDocument>.Filter.Eq("_id", student["_id"]),
                                update
                            );

                            updatedCount++;
                            createdUsersCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error updating student {student.GetValue("_id", "")}: {ex.Message}");
                }
            }

            // Step 3: Create index on userId field for better query performance
            await CreateIndexSafeAsync(database, "Students",
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("userId"),
                    new CreateIndexOptions { Name = "idx_userId" }
                ));

            // Step 4: Validate the migration
            await ValidateMigration(database);

            Console.WriteLine($"✅ Migration completed: Updated {updatedCount} students (created {createdUsersCount} new users)");
        }

        public override async Task Down(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Rolling back: Remove User Foreign Key from Students");

            if (!await CollectionExistsAsync(database, "Students"))
                return;

            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            // Remove userId field from all students
            var update = Builders<BsonDocument>.Update.Unset("userId");
            var result = await studentsCollection.UpdateManyAsync(
                Builders<BsonDocument>.Filter.Exists("userId", true),
                update
            );

            // Drop the userId index
            await DropIndexSafeAsync(database, "Students", "idx_userId");

            Console.WriteLine($"✅ Rollback completed: Removed userId field from {result.ModifiedCount} students");
        }

        private async Task<string> CreateUserFromStudent(IMongoDatabase database, BsonDocument student)
        {
            try
            {
                var usersCollection = database.GetCollection<BsonDocument>("Users");

                var newUser = new BsonDocument
                {
                    ["email"] = student.GetValue("email", ""),
                    ["firstName"] = student.GetValue("firstName", ""),
                    ["lastName"] = student.GetValue("lastName", ""),
                    ["passwordHash"] = BCrypt.Net.BCrypt.HashPassword("TempPassword123!"), // Default password
                    ["role"] = "Student",
                    ["isActive"] = true,
                    ["createdAt"] = DateTime.UtcNow,
                    ["lastLogin"] = BsonNull.Value
                };

                await usersCollection.InsertOneAsync(newUser);
                Console.WriteLine($"✅ Created new user for student: {student.GetValue("email", "")}");

                return newUser["_id"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating user for student {student.GetValue("email", "")}: {ex.Message}");
                return null;
            }
        }

        private async Task ValidateMigration(IMongoDatabase database)
        {
            Console.WriteLine("🔍 Validating migration results...");

            var studentsCollection = database.GetCollection<BsonDocument>("Students");
            var usersCollection = database.GetCollection<BsonDocument>("Users");

            // Count students with userId
            var studentsWithUserId = await studentsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Exists("userId", true)
            );

            // Count students without userId
            var studentsWithoutUserId = await studentsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Exists("userId", false)
            );

            // Check for orphaned references (students pointing to non-existent users)
            var allStudents = await studentsCollection.Find(new BsonDocument()).ToListAsync();
            int orphanedCount = 0;

            foreach (var student in allStudents)
            {
                if (student.Contains("userId"))
                {
                    var userId = student["userId"].ToString();
                    var user = await usersCollection.Find(
                        Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(userId))
                    ).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        orphanedCount++;
                        Console.WriteLine($"⚠️ Orphaned reference: Student {student.GetValue("email", "")} points to non-existent user {userId}");
                    }
                }
            }

            Console.WriteLine($"📊 Validation Results:");
            Console.WriteLine($"   Students with userId: {studentsWithUserId}");
            Console.WriteLine($"   Students without userId: {studentsWithoutUserId}");
            Console.WriteLine($"   Orphaned references: {orphanedCount}");

            if (orphanedCount > 0)
            {
                Console.WriteLine("❌ Validation failed: Found orphaned references");
                throw new Exception($"Found {orphanedCount} orphaned student-user references");
            }

            Console.WriteLine("✅ Validation passed: All references are valid");
        }
    }
}