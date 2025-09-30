using DBMigration.Services;
using DBMigration.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBMigration.Migrations
{
    public class _006_ConvertStudentIdToString_290925 : SafeMigrationBase
    {
        public override string MigrationId => "006_ConvertStudentIdToString";
        public override string Name => "Convert StudentId to String Format";
        public override string Description => "Convert studentId field from ObjectId to string format in Applications collection";

        public override async Task Up(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Starting migration: Convert studentId to string format");

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

            // Step 1: Check current state
            var sampleApp = await applicationsCollection.Find(new BsonDocument()).FirstOrDefaultAsync();
            if (sampleApp != null)
            {
                Console.WriteLine("📊 Current Application document structure:");
                foreach (var element in sampleApp.Elements)
                {
                    Console.WriteLine($"   - {element.Name}: {element.Value.BsonType} = {element.Value}");
                }
            }

            // Step 2: Get all applications
            var applications = await applicationsCollection.Find(new BsonDocument()).ToListAsync();
            Console.WriteLine($"📊 Found {applications.Count} applications to process");

            int convertedCount = 0;
            int alreadyStringCount = 0;
            int nullOrMissingCount = 0;

            foreach (var application in applications)
            {
                try
                {
                    // Check if studentId field exists and its current type
                    if (!application.Contains("studentId"))
                    {
                        Console.WriteLine($"⚠️ Application {application["_id"]} has no studentId field");
                        nullOrMissingCount++;
                        continue;
                    }

                    var studentIdValue = application["studentId"];

                    if (studentIdValue.BsonType == BsonType.String)
                    {
                        Console.WriteLine($"ℹ️ Application {application["_id"]} studentId is already string: {studentIdValue}");
                        alreadyStringCount++;
                        continue;
                    }

                    if (studentIdValue.BsonType == BsonType.ObjectId)
                    {
                        // Convert ObjectId to string
                        var objectIdString = studentIdValue.AsObjectId.ToString();

                        var update = Builders<BsonDocument>.Update.Set("studentId", objectIdString);
                        var result = await applicationsCollection.UpdateOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", application["_id"]),
                            update
                        );

                        if (result.ModifiedCount > 0)
                        {
                            Console.WriteLine($"✅ Converted studentId from ObjectId to string: {objectIdString}");
                            convertedCount++;
                        }
                    }
                    else if (studentIdValue.BsonType == BsonType.Null)
                    {
                        Console.WriteLine($"ℹ️ Application {application["_id"]} has null studentId");
                        nullOrMissingCount++;
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Application {application["_id"]} has unexpected studentId type: {studentIdValue.BsonType}");
                        nullOrMissingCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing application {application["_id"]}: {ex.Message}");
                }
            }

            // Step 3: Validate the conversion
            await ValidateMigration(database);

            Console.WriteLine($"✅ Migration completed:");
            Console.WriteLine($"   Converted: {convertedCount} applications");
            Console.WriteLine($"   Already string: {alreadyStringCount} applications");
            Console.WriteLine($"   Null/Missing: {nullOrMissingCount} applications");
        }

        public override async Task Down(IMongoDatabase database)
        {
            Console.WriteLine("🔄 Rolling back: Convert studentId back to ObjectId format");

            if (!await CollectionExistsAsync(database, "Applications"))
                return;

            var applicationsCollection = database.GetCollection<BsonDocument>("Applications");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            var applications = await applicationsCollection.Find(new BsonDocument()).ToListAsync();
            int rolledBackCount = 0;

            foreach (var application in applications)
            {
                try
                {
                    if (application.Contains("studentId") && application["studentId"].BsonType == BsonType.String)
                    {
                        var studentIdString = application["studentId"].AsString;

                        if (ObjectId.TryParse(studentIdString, out var objectId))
                        {
                            var update = Builders<BsonDocument>.Update.Set("studentId", objectId);
                            var result = await applicationsCollection.UpdateOneAsync(
                                Builders<BsonDocument>.Filter.Eq("_id", application["_id"]),
                                update
                            );

                            if (result.ModifiedCount > 0)
                            {
                                Console.WriteLine($"✅ Rolled back studentId to ObjectId: {objectId}");
                                rolledBackCount++;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Cannot parse studentId as ObjectId: {studentIdString}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error rolling back application {application["_id"]}: {ex.Message}");
                }
            }

            Console.WriteLine($"✅ Rollback completed: Converted {rolledBackCount} applications back to ObjectId");
        }

        private async Task ValidateMigration(IMongoDatabase database)
        {
            Console.WriteLine("🔍 Validating migration results...");

            var applicationsCollection = database.GetCollection<BsonDocument>("Applications");
            var studentsCollection = database.GetCollection<BsonDocument>("Students");

            // Count applications by studentId type after conversion
            var totalApplications = await applicationsCollection.CountDocumentsAsync(new BsonDocument());

            var applicationsWithString = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.String)
            );

            var applicationsWithObjectId = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.ObjectId)
            );

            var applicationsWithNull = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Eq("studentId", BsonNull.Value)
            );

            var applicationsWithoutStudentId = await applicationsCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Exists("studentId", false)
            );

            // Check if all string studentIds reference valid students
            var applicationsWithStringIds = await applicationsCollection.Find(
                Builders<BsonDocument>.Filter.Type("studentId", BsonType.String)
            ).ToListAsync();

            int validReferences = 0;
            int invalidReferences = 0;

            foreach (var application in applicationsWithStringIds)
            {
                var studentIdString = application["studentId"].AsString;

                if (ObjectId.TryParse(studentIdString, out var objectId))
                {
                    var student = await studentsCollection.Find(
                        Builders<BsonDocument>.Filter.Eq("_id", objectId)
                    ).FirstOrDefaultAsync();

                    if (student != null)
                    {
                        validReferences++;
                    }
                    else
                    {
                        invalidReferences++;
                        Console.WriteLine($"⚠️ Invalid reference: Application {application["_id"]} points to non-existent student {studentIdString}");
                    }
                }
                else
                {
                    invalidReferences++;
                    Console.WriteLine($"⚠️ Invalid ObjectId format: {studentIdString}");
                }
            }

            Console.WriteLine($"📊 Validation Results:");
            Console.WriteLine($"   Total applications: {totalApplications}");
            Console.WriteLine($"   With studentId (String): {applicationsWithString}");
            Console.WriteLine($"   With studentId (ObjectId): {applicationsWithObjectId}");
            Console.WriteLine($"   With studentId (null): {applicationsWithNull}");
            Console.WriteLine($"   Without studentId field: {applicationsWithoutStudentId}");
            Console.WriteLine($"   Valid references: {validReferences}");
            Console.WriteLine($"   Invalid references: {invalidReferences}");

            if (invalidReferences > 0)
            {
                Console.WriteLine("❌ Validation failed: Found invalid references");
                throw new Exception($"Found {invalidReferences} invalid student references");
            }

            Console.WriteLine("✅ Validation passed: All studentId fields are properly converted to string format");
        }
    }
}