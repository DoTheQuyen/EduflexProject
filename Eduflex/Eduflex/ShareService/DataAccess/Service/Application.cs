using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Application;
using ShareService.Models.Student;

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