using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _005_AddStudentForeignKeyToApplications_290925 : SafeMigrationBase
    {
        public override string MigrationId => "_005_AddStudentForeignKeyToApplications_290925";
        public override string Name => "Update Applications Student Foreign Key";
        public override string Description => "Remove old student fields and add proper studentId referencing Students _id";

        public override async Task Up(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Starting migration: Update Applications Student Foreign Key");

            // Check if collections exist
            if (!await CollectionExistsAsync(database, "Applications"))
            {
                Console.WriteLine("❌ Applications collection doesn't exist.");
                return;
            }

            if (!await CollectionExistsAsync(database, "Students"))
            {
                Console.WriteLine("❌ Students collection doesn't exist.");
                return;
            }

            var applicationsCollection = database.GetCollection<BsonDocument>("Applications");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            // Step 1: Check current state of Applications collection
            var sampleApp = await applicationsCollection.Find(new BsonDocument()).FirstOrDefaultAsync();
            if (sampleApp != null)
            {
                Console.WriteLine("📊 Current Application document structure:");
                foreach (var element in sampleApp.Elements)
                {
                    Console.WriteLine($"   - {element.Name}: {element.Value.BsonType} = {element.Value}");
                }
            }

            // Step 2: Check existing indexes
            var existingIndexes = await GetExistingIndexNamesAsync(database, "Applications");
            Console.WriteLine($"📊 Existing indexes in Applications: {string.Join(", ", existingIndexes)}");

            // Step 3: Get all students for reference
            var students = await studentsCollection.Find(new BsonDocument()).ToListAsync();
            Console.WriteLine($"📊 Found {students.Count} students in database");

            // Step 4: Get all applications
            var applications = await applicationsCollection.Find(new BsonDocument()).ToListAsync();
            Console.WriteLine($"📊 Found {applications.Count} applications to update");

            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var application in applications)
            {
                try
                {
                    // Step 5: Check what student reference fields exist and their types
                    bool hasOldStudentId = application.Contains("studentId");
                    bool hasStudentObjectId = application.Contains("studentObjectId");

                    var oldStudentIdType = hasOldStudentId ? application["studentId"].BsonType : BsonType.Null;
                    var studentObjectIdType = hasStudentObjectId ? application["studentObjectId"].BsonType : BsonType.Null;

                    Console.WriteLine($"🔍 Application {application["_id"]}: studentId={oldStudentIdType}, studentObjectId={studentObjectIdType}");

                    BsonDocument matchingStudent = null;
                    string oldStudentEmail = null;

                    // Case 1: If studentObjectId exists and is ObjectId type, use it directly
                    if (hasStudentObjectId && application["studentObjectId"].BsonType == BsonType.ObjectId)
                    {
                        var studentObjectId = application["studentObjectId"].AsObjectId;
                        matchingStudent = students.FirstOrDefault(s => s["_id"].AsObjectId == studentObjectId);
                        Console.WriteLine($"   Trying studentObjectId (ObjectId): {studentObjectId} -> {(matchingStudent != null ? "Found" : "Not found")}");
                    }
                    // Case 1b: If studentObjectId exists and is String type, parse it
                    else if (hasStudentObjectId && application["studentObjectId"].BsonType == BsonType.String)
                    {
                        if (ObjectId.TryParse(application["studentObjectId"].AsString, out var studentObjectId))
                        {
                            matchingStudent = students.FirstOrDefault(s => s["_id"].AsObjectId == studentObjectId);
                            Console.WriteLine($"   Trying studentObjectId (String): {studentObjectId} -> {(matchingStudent != null ? "Found" : "Not found")}");
                        }
                    }

                    // Case 2: If old studentId exists and is String type (email), try to match by email
                    if (matchingStudent == null && hasOldStudentId && application["studentId"].BsonType == BsonType.String)
                    {
                        oldStudentEmail = application["studentId"].AsString;
                        matchingStudent = students.FirstOrDefault(s =>
                            s.GetValue("email", "").AsString.Equals(oldStudentEmail, StringComparison.OrdinalIgnoreCase));
                        Console.WriteLine($"   Trying studentId (email): {oldStudentEmail} -> {(matchingStudent != null ? "Found" : "Not found")}");
                    }
                    // Case 2b: If old studentId exists and is ObjectId type, use it directly
                    else if (matchingStudent == null && hasOldStudentId && application["studentId"].BsonType == BsonType.ObjectId)
                    {
                        var studentId = application["studentId"].AsObjectId;
                        matchingStudent = students.FirstOrDefault(s => s["_id"].AsObjectId == studentId);
                        Console.WriteLine($"   Trying studentId (ObjectId): {studentId} -> {(matchingStudent != null ? "Found" : "Not found")}");
                    }

                    // Step 6: Prepare update operations
                    var updates = new List<UpdateDefinition<BsonDocument>>();

                    if (matchingStudent != null)
                    {
                        // Update studentId to use the Student's ObjectId
                        updates.Add(Builders<BsonDocument>.Update.Set("studentId", matchingStudent["_id"]));

                        // Backup old email if it exists and is different from current student email
                        if (!string.IsNullOrEmpty(oldStudentEmail) && matchingStudent.Contains("email"))
                        {
                            var currentStudentEmail = matchingStudent["email"].AsString;
                            if (oldStudentEmail != currentStudentEmail)
                            {
                                updates.Add(Builders<BsonDocument>.Update.Set("oldStudentEmail", oldStudentEmail));
                            }
                        }

                        updatedCount++;
                        Console.WriteLine($"   ✅ Will update to use studentId: {matchingStudent["_id"]}");
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️ No matching student found. Setting studentId to null.");
                        updates.Add(Builders<BsonDocument>.Update.Set("studentId", BsonNull.Value));
                        skippedCount++;
                    }

                    // Remove studentObjectId field if it exists
                    if (hasStudentObjectId)
                    {
                        updates.Add(Builders<BsonDocument>.Update.Unset("studentObjectId"));
                        Console.WriteLine($"   🗑️ Will remove studentObjectId field");
                    }

                    // Step 7: Apply all updates
                    if (updates.Any())
                    {
                        var combinedUpdate = Builders<BsonDocument>.Update.Combine(updates);
                        var result = await applicationsCollection.UpdateOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", application["_id"]),
                            combinedUpdate
                        );

                        if (result.ModifiedCount > 0)
                        {
                            Console.WriteLine($"   ✅ Successfully updated application");
                        }
                        else
                        {
                            Console.WriteLine($"   ⚠️ No changes made to application");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ℹ️ No updates needed for application");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error updating application: {ex.Message}");
                    skippedCount++;
                }
                Console.WriteLine(); // Empty line for readability
            }

            // Step 8: No need to create index - studentId_1 already exists
            Console.WriteLine("ℹ️ Index studentId_1 already exists - no need to create new index");

            // Step 9: Drop the old idx_studentObjectId index if it exists
            if (existingIndexes.Contains("idx_studentObjectId"))
            {
                await DropIndexSafeAsync(database, "Applications", "idx_studentObjectId");
                Console.WriteLine("✅ Dropped old idx_studentObjectId index");
            }

            // Step 10: Validate the migration
            await ValidateMigration(database);

            Console.WriteLine($"✅ Migration completed: Updated {updatedCount} applications, skipped {skippedCount}");
        }

        public override async Task Down(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Rolling back: Restore old student fields in Applications");

            if (!await CollectionExistsAsync(database, "Applications"))
                return;

            var applicationsCollection = database.GetCollection<BsonDocument>("Applications");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            var applications = await applicationsCollection.Find(new BsonDocument()).ToListAsync();
            var students = await studentsCollection.Find(new BsonDocument()).ToListAsync();

            int rolledBackCount = 0;

            foreach (var application in applications)
            {
                try
                {
                    var updates = new List<UpdateDefinition<BsonDocument>>();

                    // If we have oldStudentEmail backup, restore it as studentId
                    if (application.Contains("oldStudentEmail"))
                    {
                        var oldEmail = application["oldStudentEmail"].AsString;
                        updates.Add(Builders<BsonDocument>.Update.Set("studentId", oldEmail));
                        updates.Add(Builders<BsonDocument>.Update.Unset("oldStudentEmail"));
                        Console.WriteLine($"   🔄 Restoring old studentId (email): {oldEmail}");
                    }
                    else if (application.Contains("studentId") && application["studentId"].BsonType == BsonType.ObjectId)
                    {
                        // Try to find student email from studentId
                        var studentId = application["studentId"].AsObjectId;
                        var student = students.FirstOrDefault(s => s["_id"].AsObjectId == studentId);
                        if (student != null && student.Contains("email"))
                        {
                            updates.Add(Builders<BsonDocument>.Update.Set("studentId", student["email"].AsString));
                            Console.WriteLine($"   🔄 Deriving email from student: {student["email"].AsString}");
                        }
                    }

                    // Remove the backup field if it exists
                    if (application.Contains("oldStudentEmail"))
                    {
                        updates.Add(Builders<BsonDocument>.Update.Unset("oldStudentEmail"));
                    }

                    if (updates.Any())
                    {
                        var combinedUpdate = Builders<BsonDocument>.Update.Combine(updates);
                        var result = await applicationsCollection.UpdateOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", application["_id"]),
                            combinedUpdate
                        );

                        if (result.ModifiedCount > 0)
                        {
                            rolledBackCount++;
                            Console.WriteLine($"   ✅ Successfully rolled back application");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error rolling back application {application.GetValue("_id", "")}: {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Rollback completed: Restored {rolledBackCount} applications");
        }

        private async Task ValidateMigration(IMongoDatabase database)
        {
            Console.WriteLine("🔍 Validating migration results...");

            var applicationsCollection = database.GetCollection<BsonDocument>("Applications");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            // Count applications by studentId type
            var totalApplications = await applicationsCollection.CountDocumentsAsync(new BsonDocument());

            var applicationsWithObjectId = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.ObjectId)
            );

            var applicationsWithString = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.String)
            );

            var applicationsWithNull = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Eq("studentId", BsonNull.Value)
            );

            var applicationsWithoutStudentId = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Exists("studentId", false)
            );

            // Check for orphaned references
            var applicationsWithObjectIds = await applicationsCollection.Find(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.ObjectId)
            ).ToListAsync();

            int orphanedCount = 0;
            foreach (var application in applicationsWithObjectIds)
            {
                var studentId = application["studentId"].AsObjectId;
                var student = await studentsCollection.Find(
                    Builders<BsonDocument>.Filter.Eq("_id", studentId)
                ).FirstOrDefaultAsync();

                if (student == null)
                {
                    orphanedCount++;
                    Console.WriteLine($"⚠️ Orphaned reference: Application {application["_id"]} points to non-existent student {studentId}");
                }
            }

            Console.WriteLine($"📊 Validation Results:");
            Console.WriteLine($"   Total applications: {totalApplications}");
            Console.WriteLine($"   With studentId (ObjectId): {applicationsWithObjectId}");
            Console.WriteLine($"   With studentId (String/email): {applicationsWithString}");
            Console.WriteLine($"   With studentId (null): {applicationsWithNull}");
            Console.WriteLine($"   Without studentId field: {applicationsWithoutStudentId}");
            Console.WriteLine($"   Orphaned references: {orphanedCount}");

            if (orphanedCount > 0)
            {
                Console.WriteLine("❌ Validation failed: Found orphaned references");
                throw new Exception($"Found {orphanedCount} orphaned application-student references");
            }

            Console.WriteLine("✅ Validation passed");
        }
    }
}