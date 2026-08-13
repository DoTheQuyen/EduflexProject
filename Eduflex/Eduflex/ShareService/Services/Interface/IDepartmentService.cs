using ShareService.Common;
using ShareService.Models.Department;

namespace ShareService.Services.Interface
{
    public interface IDepartmentService
    {
        // Auth: none — internal plumbing / directory lookups (e.g. resolving department
        // badges for the Users list, populating a parent-department picker). Mirrors
        // IRoleService.GetAllRolesAsync.
        Task<List<DepartmentModel>> GetAllDepartmentsAsync();

        // Auth: none — internal plumbing, same as GetAllDepartmentsAsync. Used by
        // TaskItemService to scope the Manager's "All Tasks" page to the department(s)
        // this user heads; returns an empty list for anyone who isn't a HeadUserId
        // anywhere (a plain Staff member manages no department).
        Task<List<DepartmentModel>> GetDepartmentsManagedByUserAsync(string userId);

        Task<DepartmentModel?> GetDepartmentByIdAsync(string id, string userId);
        Task<PagedResult<DepartmentModel>> SearchDepartmentsAsync(DepartmentFilter filter, string userId);
        Task<bool> CreateDepartmentAsync(DepartmentModel department, string userId);
        Task<bool> UpdateDepartmentAsync(string id, DepartmentModel department, string userId);
        Task<bool> DeleteDepartmentAsync(string id, string userId);
    }
}
