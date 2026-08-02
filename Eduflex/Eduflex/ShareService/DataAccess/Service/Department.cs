using MongoDB.Driver;
using ShareService.Common;
using ShareService.DataAccess.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Department;

namespace ShareService.DataAccess
{
    public class Department : AuditableCollectionBase<DepartmentModel>, IDepartment
    {
        public Department(IMongoDatabase database, ICurrentUserService currentUser)
            : base(database.GetCollection<DepartmentModel>("Departments"), currentUser)
        {
        }

        public async Task<bool> CreateDepartmentAsync(DepartmentModel department)
        {
            await InsertOneAsync(department);
            return true;
        }

        public async Task<DepartmentModel?> GetDepartmentByIdAsync(string id)
        {
            return await Collection.Find(d => d.Id == id).FirstOrDefaultAsync();
        }

        public async Task<DepartmentModel?> GetByNameAsync(string name)
        {
            return await Collection.Find(d => d.Name == name).FirstOrDefaultAsync();
        }

        public async Task<List<DepartmentModel>> GetAllAsync()
        {
            return await Collection.Find(FilterDefinition<DepartmentModel>.Empty).ToListAsync();
        }

        public Task<PagedResult<DepartmentModel>> GetDepartmentsAsync(DepartmentFilter filter)
        {
            var mongoFilter = BuildSearchFilter(filter.SearchTerm, d => d.Name);
            var sort = Builders<DepartmentModel>.Sort.Ascending(d => d.Name);

            return GetPagedAsync(mongoFilter, sort, filter.PageNumber, filter.PageSize);
        }

        public async Task<bool> UpdateDepartmentAsync(string id, DepartmentModel department)
        {
            return await ReplaceOneAsync(d => d.Id == id, department);
        }

        public async Task<bool> DeleteDepartmentAsync(string id)
        {
            var result = await Collection.DeleteOneAsync(d => d.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> HasChildDepartmentsAsync(string departmentId)
        {
            return await Collection.Find(d => d.ParentDepartmentId == departmentId).AnyAsync();
        }
    }
}
