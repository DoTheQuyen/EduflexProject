using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models;

namespace ShareService.DataAccess
{
    public class Application : IApplication
    {
        private readonly IMongoCollection<ApplicationModel> _applicationsCollection;
        private readonly IMongoCollection<StudentModel> _studentsCollection;

        public Application(IMongoDatabase database)
        {
            _applicationsCollection = database.GetCollection<ApplicationModel>("Applications");
            _studentsCollection = database.GetCollection<StudentModel>("Students");

            // Indexes already exist in the database, no need to create them here
        }

        public async Task<StudentModel?> GetStudentByUserIdAsync(string userId)
        {
            return await _studentsCollection
             .Find(s => s.UserId == userId)
             .FirstOrDefaultAsync();
        }

        public async Task DebugDatabaseContent(string userId)
        {
            try
            {
                Console.WriteLine("=== DEBUGGING DATABASE CONTENT ===");
                Console.WriteLine($"Searching for userId: {userId}");

                // Get the raw BSON documents
                var database = _studentsCollection.Database;
                var studentsCollection = database.GetCollection<BsonDocument>("Students");

                var allStudents = await studentsCollection.Find(new BsonDocument()).ToListAsync();

                Console.WriteLine($"Total students in database: {allStudents.Count}");

                foreach (var studentDoc in allStudents)
                {
                    Console.WriteLine("--- RAW STUDENT DOCUMENT ---");
                    Console.WriteLine(studentDoc.ToJson());

                    // Check if userId field exists and what type it is
                    if (studentDoc.Contains("userId"))
                    {
                        var userIdValue = studentDoc["userId"];
                        Console.WriteLine($"userId field - BsonType: {userIdValue.BsonType}, Value: {userIdValue}");

                        // Try to match with our search userId
                        if (userIdValue.BsonType == BsonType.ObjectId)
                        {
                            var objectId = userIdValue.AsObjectId;
                            Console.WriteLine($"ObjectId: {objectId}, AsString: {objectId.ToString()}");

                            if (ObjectId.TryParse(userId, out var searchObjectId))
                            {
                                Console.WriteLine($"Match: {objectId == searchObjectId}");
                            }
                        }
                        else if (userIdValue.BsonType == BsonType.String)
                        {
                            var stringValue = userIdValue.AsString;
                            Console.WriteLine($"String value: '{stringValue}'");
                            Console.WriteLine($"Match: {stringValue == userId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("NO userId FIELD FOUND IN THIS DOCUMENT!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Debug error: {ex.Message}");
            }
        }

        public async Task<List<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId)
        {
            return await _applicationsCollection
                .Find(a => a.StudentId == studentId)
                .SortByDescending(a => a.DateApplied)
                .ToListAsync();
        }

        public async Task<ApplicationModel?> GetApplicationByIdAsync(string id)
        {
            return await _applicationsCollection
                .Find(a => a.Id == id)
                .FirstOrDefaultAsync();
        }


        /// <summary>
        /// practice apply transaction session
        /// </summary>
        /// <param name="application"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public async Task<ApplicationModel> CreateApplicationAsync(ApplicationModel application, IClientSessionHandle? session = null)
        {
            if (session == null)
                await _applicationsCollection.InsertOneAsync(application);
            else
                await _applicationsCollection.InsertOneAsync(session, application);
            return application;
        }

        public async Task<bool> UpdateApplicationStatusAsync(string id, string status, IClientSessionHandle? session = null)
        {
            var update = Builders<ApplicationModel>.Update
                .Set(a => a.Status, status);

            var result = session == null
                ? await _applicationsCollection.UpdateOneAsync(a => a.Id == id, update)
                : await _applicationsCollection.UpdateOneAsync(session, a => a.Id == id, update);

            return result.ModifiedCount > 0;
        }
    }
}