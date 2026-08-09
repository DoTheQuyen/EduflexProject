using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Mapping;
using ShareService.Models.Department;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartment _departmentDataAccess;
        private readonly IUserDB _userDB;
        private readonly IRoleService _roleService;
        private readonly IValidator<DepartmentModel> _departmentValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            IDepartment departmentDataAccess,
            IUserDB userDB,
            IRoleService roleService,
            IValidator<DepartmentModel> departmentValidator,
            IPermissionService permissionService,
            ILogger<DepartmentService> logger)
        {
            _departmentDataAccess = departmentDataAccess;
            _userDB = userDB;
            _roleService = roleService;
            _departmentValidator = departmentValidator;
            _permissionService = permissionService;
            _logger = logger;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        // Server-side enforcement, independent of what the UI happens to offer — a direct
        // API call with a student's user id must be rejected the same as a UI-driven one.
        // Resolves each member's role the same way UsersController.SearchUsers resolves
        // RoleName: batch-fetch the users, batch-fetch all roles, join by RoleId.
        private async Task CheckNoStudentMembersAsync(List<string> memberUserIds)
        {
            if (memberUserIds == null || memberUserIds.Count == 0)
            {
                return;
            }

            var members = await _userDB.GetUsersByIdsAsync(memberUserIds);
            var roles = await _roleService.GetAllRolesAsync();
            var roleTypeById = roles.ToDictionary(r => r.Id, r => r.RoleType);

            var hasStudent = members.Any(m =>
                roleTypeById.TryGetValue(m.RoleId ?? string.Empty, out var roleType) && roleType == RoleTypeEnums.Student);

            if (hasStudent)
            {
                throw new ArgumentException("Students cannot be assigned to a department.");
            }
        }

        // Auth: none — see interface doc comment.
        public async Task<List<DepartmentModel>> GetAllDepartmentsAsync()
        {
            try
            {
                return await _departmentDataAccess.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all departments");
                throw;
            }
        }

        // Auth: requires DepartmentsView permission (staff detail page).
        public async Task<DepartmentModel?> GetDepartmentByIdAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.DepartmentsView, "view departments");
            return await _departmentDataAccess.GetDepartmentByIdAsync(id);
        }

        // Auth: requires DepartmentsView permission (staff-only).
        public async Task<PagedResult<DepartmentModel>> SearchDepartmentsAsync(DepartmentFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.DepartmentsView, "view departments");
            return await _departmentDataAccess.GetDepartmentsAsync(filter);
        }

        // Auth: requires DepartmentsAdd permission (staff-only).
        public async Task<bool> CreateDepartmentAsync(DepartmentModel department, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.DepartmentsAdd, "create departments");

                var validate = await _departmentValidator.ValidateAsync(department);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                var existing = await _departmentDataAccess.GetByNameAsync(department.Name);
                if (existing != null)
                {
                    throw new ArgumentException($"A department named '{department.Name}' already exists");
                }

                if (!string.IsNullOrEmpty(department.ParentDepartmentId))
                {
                    var parent = await _departmentDataAccess.GetDepartmentByIdAsync(department.ParentDepartmentId);
                    if (parent == null)
                    {
                        throw new ArgumentException("Parent department not found");
                    }
                }

                await CheckNoStudentMembersAsync(department.MemberUserIds);

                department.Id = string.Empty;
                department.MemberUserIds ??= new List<string>();

                return await _departmentDataAccess.CreateDepartmentAsync(department);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department: {DepartmentName}", department.Name);
                throw;
            }
        }

        // Auth: requires DepartmentsEdit permission (staff-only).
        public async Task<bool> UpdateDepartmentAsync(string id, DepartmentModel department, string userId)
        {
            try
            {
                await RequirePermissionAsync(userId, PermissionKey.DepartmentsEdit, "update departments");

                var existing = await _departmentDataAccess.GetDepartmentByIdAsync(id);
                if (existing == null)
                {
                    throw new ArgumentException("Department not found");
                }

                // Validated against the *target* id/parent, not the blank model FluentValidation
                // would otherwise see, so the "cannot be its own parent" rule actually fires.
                department.Id = id;
                var validate = await _departmentValidator.ValidateAsync(department);
                if (!validate.IsValid)
                {
                    var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                    throw new ArgumentException($"Validation failed: {errors}");
                }

                if (!string.IsNullOrEmpty(department.ParentDepartmentId))
                {
                    var parent = await _departmentDataAccess.GetDepartmentByIdAsync(department.ParentDepartmentId);
                    if (parent == null)
                    {
                        throw new ArgumentException("Parent department not found");
                    }
                }

                await CheckNoStudentMembersAsync(department.MemberUserIds);

                existing.ApplyEditableFields(department);

                return await _departmentDataAccess.UpdateDepartmentAsync(existing.Id, existing);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department: {DepartmentId}", id);
                throw;
            }
        }

        // Auth: requires DepartmentsDelete permission (staff-only). Blocked if any other
        // department still points at this one as its parent — deleting it would orphan
        // that department's ParentDepartmentId. Re-parent the children first.
        public async Task<bool> DeleteDepartmentAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.DepartmentsDelete, "delete departments");

            if (await _departmentDataAccess.HasChildDepartmentsAsync(id))
            {
                throw new ArgumentException("Cannot delete: one or more departments are still nested under this department.");
            }

            var deleted = await _departmentDataAccess.DeleteDepartmentAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted department with ID: {DepartmentId}", id);
            }
            return deleted;
        }
    }
}
