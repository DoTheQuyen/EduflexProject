using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Student;

namespace ShareService.DataAccess
{
    public class StudentDB : AuditableCollectionBase<StudentModel>, IStudentDB
    {
        public StudentDB(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<StudentModel>("Students"), currentUser)
        {
        }

        public async Task<StudentModel?> GetByIdAsync(string id)
        {
            return await Collection
                .Find(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentModel?> GetByUserIdAsync(string userId)
        {
            return await Collection
                .Find(s => s.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentModel?> GetByEmailAsync(string email)
        {
            return await Collection
                .Find(s => s.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentModel?> GetByPassportNumberAsync(string passportNumber)
        {
            return await Collection
                .Find(s => s.PassportNumber == passportNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentModel?> GetByDateOfBirthAsync(DateTime dateOfBirth)
        {
            return await Collection
                .Find(s => s.DateOfBirth == dateOfBirth)
                .FirstOrDefaultAsync();
        }

        public async Task<StudentModel> CreateAsync(StudentModel student)
        {
            await InsertOneAsync(student);
            return student;
        }

        public async Task<bool> UpdateAsync(string id, StudentModel student)
        {
            return await ReplaceOneAsync(s => s.Id == id, student);
        }

        public Task<PagedResult<StudentModel>> SearchAsync(StudentFilter filter, List<string>? restrictToUserIds)
        {
            var mongoFilters = new List<FilterDefinition<StudentModel>>
            {
                BuildSearchFilter(filter.SearchTerm, s => s.Email, s => s.FirstName, s => s.LastName, s => s.PassportNumber, s => s.PhoneNumber)
            };

            if (restrictToUserIds != null)
            {
                mongoFilters.Add(Builders<StudentModel>.Filter.In(s => s.UserId, restrictToUserIds));
            }

            if (filter.Type.HasValue)
            {
                mongoFilters.Add(Builders<StudentModel>.Filter.Eq(s => s.Type, filter.Type.Value));
            }

            var mongoFilter = Builders<StudentModel>.Filter.And(mongoFilters);
            var sort = Builders<StudentModel>.Sort.Descending(s => s.CreatedAt);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }
    }
}
