using ShareService.Common;
using ShareService.Models.Department;

namespace ShareService.DataAccess.Interface
{
    public interface IDepartment
    {
        Task<bool> CreateDepartmentAsync(DepartmentModel department);
        Task<DepartmentModel?> GetDepartmentByIdAsync(string id);
        Task<DepartmentModel?> GetByNameAsync(string name);
        Task<List<DepartmentModel>> GetAllAsync();
        Task<PagedResult<DepartmentModel>> GetDepartmentsAsync(DepartmentFilter filter);
        Task<bool> UpdateDepartmentAsync(string id, DepartmentModel department);
        Task<bool> DeleteDepartmentAsync(string id);
        Task<bool> HasChildDepartmentsAsync(string departmentId);
    }
}
