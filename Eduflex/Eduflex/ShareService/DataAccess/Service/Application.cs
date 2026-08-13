using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.Common;
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

        public async Task<PagedResult<ApplicationModel>> GetApplicationsByStudentIdAsync(string studentId, PaginationQuery query)
        {
            var filter = Builders<ApplicationModel>.Filter.Eq(a => a.StudentId, studentId);

            var totalCount = await _applicationsCollection.CountDocumentsAsync(filter);

            var items = await _applicationsCollection
                .Find(filter)
                .SortByDescending(a => a.DateApplied)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Limit(query.PageSize)
                .ToListAsync();

            return new PagedResult<ApplicationModel>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<PagedResult<ApplicationModel>> GetAllApplicationsAsync(PaginationQuery query)
        {
            var totalCount = await _applicationsCollection.CountDocumentsAsync(FilterDefinition<ApplicationModel>.Empty);

            var items = await _applicationsCollection
                .Find(FilterDefinition<ApplicationModel>.Empty)
                .SortByDescending(a => a.DateApplied)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Limit(query.PageSize)
                .ToListAsync();

            return new PagedResult<ApplicationModel>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
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

        public async Task<StudentModel> CreateStudentAsync(StudentModel student, IClientSessionHandle? session = null)
        {
            if (session == null)
                await _studentsCollection.InsertOneAsync(student);
            else
                await _studentsCollection.InsertOneAsync(session, student);
            return student;
        }

        public async Task<int> CountApplicationsByStatusAsync(string status)
        {
            var filter = Builders<ApplicationModel>.Filter.Eq(a => a.Status, status);
            var count = await _applicationsCollection.CountDocumentsAsync(filter);
            return (int)count;
        }
    }
}
