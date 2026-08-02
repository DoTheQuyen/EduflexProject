using Eduflex.DTOs.Department;
using ShareService.Models.Department;

namespace Eduflex.Mapping.Department
{
    public static class DepartmentMappingExtension
    {
        public static DepartmentFilter ToFilter(this DepartmentFilterDto dto)
        {
            return new DepartmentFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                SearchTerm = dto.SearchTerm
            };
        }

        public static DepartmentDto ToDto(this DepartmentModel model)
        {
            return new DepartmentDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ParentDepartmentId = model.ParentDepartmentId,
                HeadUserId = model.HeadUserId,
                MemberUserIds = model.MemberUserIds,
                CreatedAt = model.CreatedAt
            };
        }

        public static DepartmentModel ToModel(this CreateDepartmentDto dto)
        {
            return new DepartmentModel
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentDepartmentId = dto.ParentDepartmentId,
                HeadUserId = dto.HeadUserId,
                MemberUserIds = dto.MemberUserIds
            };
        }

        // Every department this user belongs to, as a badge — IsHead true where the
        // department's HeadUserId matches. Used by UsersController.SearchUsers to populate
        // UserSummaryDto.Departments; centralized here rather than inline in the controller
        // so any other caller that needs the same projection doesn't reimplement it.
        public static List<DepartmentBadgeDto> ToBadges(this IEnumerable<DepartmentModel> departments, string userId)
        {
            return departments
                .Where(d => d.MemberUserIds.Contains(userId))
                .Select(d => new DepartmentBadgeDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    IsHead = d.HeadUserId == userId
                })
                .ToList();
        }
    }
}
