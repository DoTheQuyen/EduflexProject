using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Application;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplication _applicationDataAccess;
        private readonly IValidator<ApplicationModel> _createApplicationValidator;
        private readonly ILogger<ApplicationService> _logger;
        private readonly IPermissionService _permissionService;
        private readonly INotificationPublisher _notificationPublisher;

        public ApplicationService(
            IApplication applicationDataAccess,
            IValidator<ApplicationModel> createApplicationValidator,
            ILogger<ApplicationService> logger,
            IPermissionService permissionService,
            INotificationPublisher notificationPublisher)
        {
            _applicationDataAccess = applicationDataAccess;
            _createApplicationValidator = createApplicationValidator;
            _logger = logger;
            _permissionService = permissionService;
            _notificationPublisher = notificationPublisher;
        }

        public async Task<List<ApplicationModel>> GetApplicationsByStudentId(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    throw new ArgumentException("Student ID cannot be empty");
                }

                var applications = await _applicationDataAccess.GetApplicationsByStudentIdAsync(studentId);

                var result = applications.Select(a => new ApplicationModel
                {
                    Id = a.Id,
                    Description = a.Description,
                    DateApplied = a.DateApplied,
                    Status = a.Status,
                    ApplicationType = a.ApplicationType
                }).ToList();

                _logger.LogInformation("Returned {Count} applications for student {StudentId}", result.Count, studentId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationsByStudentId for student {StudentId}", studentId);
                throw new Exception("Error retrieving applications", ex);
            }
        }

        // Auth: requires ApplicationsView permission (staff-only) — used by the Student
        // Details page's "Active applications" list. Unlike the legacy
        // GetApplicationsByStudentId (internal, unguarded), this is the permission-checked
        // entry point new code should call.
        public async Task<List<ApplicationModel>> GetApplicationsForStudentAsync(string studentId, string actingUserId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.ApplicationsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view applications");
            }

            return await _applicationDataAccess.GetApplicationsByStudentIdAsync(studentId);
        }

        public async Task<PagedResult<ApplicationModel>> GetApplicationsByUserId(string userId, PaginationQuery query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("User ID cannot be empty");
                }

                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsView.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to view applications");
                }

                var student = await _applicationDataAccess.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for user ID: {UserId}", userId);
                    return new PagedResult<ApplicationModel> { PageNumber = query.PageNumber, PageSize = query.PageSize };
                }

                var result = await _applicationDataAccess.GetApplicationsByStudentIdAsync(student.Id, query);

                result.Items = result.Items.Select(a => new ApplicationModel
                {
                    Id = a.Id,
                    Description = a.Description,
                    DateApplied = a.DateApplied,
                    Status = a.Status,
                    ApplicationType = a.ApplicationType
                }).ToList();

                _logger.LogInformation("Returned {Count} applications for user {UserId} (student: {StudentId})",
                    result.Items.Count, userId, student.Id);

                return result;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationsByUserId for user {UserId}", userId);
                throw new Exception("Error retrieving applications", ex);
            }
        }

        // Auth: requires ApplicationsView permission (staff-only), no ownership check — staff
        // can view any application. Backs the "View application" link on the Student Details
        // page and the Enrolment history panel's "From application" link.
        public async Task<ApplicationDetailModel?> GetApplicationDetailForStaffAsync(string id, string actingUserId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.ApplicationsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view applications");
            }

            var application = await _applicationDataAccess.GetApplicationByIdAsync(id);
            if (application == null) return null;

            return new ApplicationDetailModel
            {
                Id = application.Id,
                StudentId = application.StudentId,
                StudentName = application.StudentName,
                Description = application.Description,
                DateApplied = application.DateApplied,
                Status = application.Status,
                Details = application.Details,
                ApplicationType = application.ApplicationType,
                StudyMode = application.StudyMode,
                Campus = application.Campus,
                HometownAddress = application.HometownAddress,
                CurrentAddress = application.CurrentAddress,
                EmergencyContact = application.EmergencyContact
            };
        }

        public async Task<ApplicationDetailModel?> GetApplicationById(string id, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("Application ID and User ID cannot be empty");
                }

                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsView.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to view applications");
                }

                var student = await _applicationDataAccess.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for user ID: {UserId}", userId);
                    return null;
                }

                var application = await _applicationDataAccess.GetApplicationByIdAsync(id);
                if (application == null)
                {
                    _logger.LogInformation("Application not found with ID: {ApplicationId}", id);
                    return null;
                }

                if (application.StudentId != student.Id)
                {
                    _logger.LogWarning("User {UserId} attempted to access application {ApplicationId} that doesn't belong to them",
                        userId, id);
                    throw new UnauthorizedAccessException("Access denied to this application");
                }

                var result = new ApplicationDetailModel
                {
                    Id = application.Id,
                    StudentId = application.StudentId,
                    StudentName = application.StudentName,
                    Description = application.Description,
                    DateApplied = application.DateApplied,
                    Status = application.Status,
                    Details = application.Details,
                    ApplicationType = application.ApplicationType,
                    StudyMode = application.StudyMode,
                    Campus = application.Campus,
                    HometownAddress = application.HometownAddress,
                    CurrentAddress = application.CurrentAddress,
                    EmergencyContact = application.EmergencyContact
                };

                _logger.LogInformation("Retrieved application details for ID: {ApplicationId} by user {UserId}", id, userId);
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationById for application {ApplicationId} by user {UserId}", id, userId);
                throw new Exception("Error retrieving application details", ex);
            }
        }

        public async Task<ApplicationModel> CreateApplication(ApplicationModel application, string userId)
        {
            var validate = await _createApplicationValidator.ValidateAsync(application);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for application creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            try
            {
                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsAdd.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to create applications");
                }

                application.Id = string.Empty;
                application.DateApplied = DateTime.UtcNow;
                application.Status = "Pending";

                var createdApplication = await _applicationDataAccess.CreateApplicationAsync(application);

                var result = new ApplicationModel
                {
                    Id = createdApplication.Id,
                    Description = createdApplication.Description,
                    DateApplied = createdApplication.DateApplied,
                    Status = createdApplication.Status,
                    ApplicationType = createdApplication.ApplicationType
                };

                await _notificationPublisher.PublishToRoleAsync(
                    module: "Application",
                    entityId: result.Id,
                    summary: $"New application submitted ({result.ApplicationType})",
                    role: SystemRole.Staff);

                _logger.LogInformation("Created new application with ID: {ApplicationId} for student {StudentId}",
                    result.Id, application.StudentId);

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApplication for student {StudentId}", application.StudentId);
                throw new Exception("Error creating application", ex);
            }
        }

        public async Task<bool> UpdateApplicationStatus(string id, string status, string userId)
        {
            try
            {
                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsEdit.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to update application status");
                }

                if (!isAValidStatus(status))
                {
                    throw new ArgumentException("Status must be one of: Pending, Approved, Rejected, Studying");
                }

                var result = await _applicationDataAccess.UpdateApplicationStatusAsync(id, status);

                if (result)
                {
                    await _notificationPublisher.PublishToRoleAsync(
                        module: "Application",
                        entityId: id,
                        summary: $"Application status changed to {status}",
                        role: SystemRole.Manager);
                }

                _logger.LogInformation("Status update for application {ApplicationId}: {Status} - Success: {Success}",
                    id, status, result);

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicationStatus for application {ApplicationId}", id);
                throw new Exception("Error updating application status", ex);
            }
        }

        private bool isAValidStatus(string status)
        {
            return status == "Pending" || status == "Approved" || status == "Rejected" || status == "Studying";
        }
    }
}