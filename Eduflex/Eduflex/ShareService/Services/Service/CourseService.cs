using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Course;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourse _courseDataAccess;
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly IValidator<CourseModel> _courseValidator;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<CourseService> _logger;

        public CourseService(
            ICourse courseDataAccess,
            IEducationPartner educationPartnerDataAccess,
            IValidator<CourseModel> courseValidator,
            IPermissionService permissionService,
            ILogger<CourseService> logger)
        {
            _courseDataAccess = courseDataAccess;
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _courseValidator = courseValidator;
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

        // Auth: requires EducationPartnersAdd permission (staff-only — courses share
        // Education Partners' permission keys rather than having their own module).
        public async Task<bool> CreateCourse(CourseModel course, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersAdd, "create courses");
            await ValidateAsync(course);

            course.Id = string.Empty;
            var created = await _courseDataAccess.CreateCourseAsync(course);
            _logger.LogInformation("Created new course with ID: {CourseId} for {CourseName}", course.Id, course.CourseName);
            return created;
        }

        // Auth: requires EducationPartnersEdit permission (staff-only).
        public async Task<bool> UpdateCourse(string id, CourseModel course, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersEdit, "update courses");
            await ValidateAsync(course);

            var existing = await _courseDataAccess.GetCourseByIdAsync(id);
            if (existing == null)
            {
                throw new ArgumentException("Course not found");
            }

            existing.CourseName = course.CourseName;
            existing.Intakes = course.Intakes;
            existing.StudyModes = course.StudyModes;
            existing.Campuses = course.Campuses;
            existing.TuitionFee = course.TuitionFee;
            existing.TotalTuitionFee = course.TotalTuitionFee;
            existing.TuitionCurrency = course.TuitionCurrency;
            existing.CourseDurationMonths = course.CourseDurationMonths;
            existing.CommissionBaseRate = course.CommissionBaseRate;

            var updated = await _courseDataAccess.UpdateCourseAsync(id, existing);
            if (updated)
            {
                _logger.LogInformation("Updated course with ID: {CourseId}", id);
            }
            return updated;
        }

        // Auth: requires EducationPartnersDelete permission (staff-only).
        public async Task<bool> DeleteCourse(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersDelete, "delete courses");

            var deleted = await _courseDataAccess.DeleteCourseAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted course with ID: {CourseId}", id);
            }
            return deleted;
        }

        // Auth: requires EducationPartnersView permission — this is the staff detail-page
        // path (CoursesController.GetCoursesByPartner). The plural GetCoursesByPartnerIds
        // below is the separate, deliberately-open path used by the student directory.
        public async Task<List<CourseModel>> GetCoursesByPartnerId(string partnerId, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersView, "view courses");
            return await _courseDataAccess.GetByPartnerIdAsync(partnerId);
        }

        // Auth: none — deliberately shared/unchecked. Used by both the open student
        // directory (EducationPartnersController.GetEducationPartnersDirectory) and the
        // staff search (which gates access one level up, at SearchEducationPartners).
        // Do not add a permission check here without checking both callers.
        public async Task<Dictionary<string, List<CourseModel>>> GetCoursesByPartnerIds(IEnumerable<string> partnerIds)
        {
            var courses = await _courseDataAccess.GetByPartnerIdsAsync(partnerIds);
            return courses
                .GroupBy(c => c.EducationPartnerId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // Auth: requires EducationPartnersView permission (staff-only).
        public async Task<PagedResult<CourseSearchResult>> SearchCourses(CourseSearchFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EducationPartnersView, "view courses");

            var allCourses = await _courseDataAccess.GetAllAsync();
            var partnerIds = allCourses.Select(c => c.EducationPartnerId).Distinct();
            var partners = await _educationPartnerDataAccess.GetByIdsAsync(partnerIds);
            var partnerById = partners.ToDictionary(p => p.Id);

            var joined = allCourses
                .Where(c => partnerById.ContainsKey(c.EducationPartnerId))
                .Select(c => new CourseSearchResult { Course = c, Partner = partnerById[c.EducationPartnerId] })
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.CourseName))
            {
                joined = joined.Where(x => x.Course.CourseName.Contains(filter.CourseName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.UniName))
            {
                joined = joined.Where(x => x.Partner.Name.Contains(filter.UniName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Country))
            {
                joined = joined.Where(x => x.Partner.Country.Contains(filter.Country, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Intake))
            {
                joined = joined.Where(x => x.Course.Intakes.Any(i => i.Contains(filter.Intake, StringComparison.OrdinalIgnoreCase)));
            }

            var ordered = joined.OrderByDescending(x => x.Course.TuitionFee).ToList();

            var totalCount = ordered.Count;
            var pageItems = ordered.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            return new PagedResult<CourseSearchResult>
            {
                Items = pageItems,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        private async Task ValidateAsync(CourseModel course)
        {
            var validate = await _courseValidator.ValidateAsync(course);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for course: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var partner = await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(course.EducationPartnerId);
            if (partner == null)
            {
                throw new ArgumentException("Education partner not found");
            }
        }
    }
}
