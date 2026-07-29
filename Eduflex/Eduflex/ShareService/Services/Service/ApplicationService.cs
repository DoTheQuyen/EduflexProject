using FluentValidation;
using Microsoft.Extensions.Logging;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
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

        public ApplicationService(
            IApplication applicationDataAccess,
            IValidator<ApplicationModel> createApplicationValidator,
            ILogger<ApplicationService> logger,
            IPermissionService permissionService)
        {
            _applicationDataAccess = applicationDataAccess;
            _createApplicationValidator = createApplicationValidator;
            _logger = logger;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Get applications by student ID with business logic processing
        /// </summary>
        /// <param name="studentId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        // Auth: none — and unlike the other methods here, that's not a deliberate "open"
        // decision, it's because nothing in the codebase currently calls this method at
        // all (dead code). Add a permission check before wiring up a real caller.
        public async Task<List<ApplicationModel>> GetApplicationsByStudentId(string studentId)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    throw new ArgumentException("Student ID cannot be empty");
                }

                // Authorization here (future implementation)

                // Process business rule here
                var applications = await _applicationDataAccess.GetApplicationsByStudentIdAsync(studentId);

                // Business logic: Return limited information for list view
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

        // Auth: requires ApplicationsView permission. Results are further scoped to the
        // caller's own student record — this is a "list my applications" endpoint, not a
        // staff-wide search (see the known gap noted in GetApplicationsByStudentId below).
        public async Task<PagedResult<ApplicationModel>> GetApplicationsByUserId(string userId, PaginationQuery query)
        {
            try
            {
                // Validation
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

                // Process business rule here
                var result = await _applicationDataAccess.GetApplicationsByStudentIdAsync(student.Id, query);

                // Business logic: Return limited information for list view
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

        /// <summary>
        /// Get application details by ID with validation and business logic
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        // Auth: requires ApplicationsView permission, AND ownership — the caller must be
        // the student the application belongs to. Both checks are needed: permission
        // alone can't tell you whose application this is.
        public async Task<ApplicationDetailModel?> GetApplicationById(string id, string userId)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("Application ID and User ID cannot be empty");
                }

                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsView.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to view applications");
                }

                // Get student by userId for authorization
                var student = await _applicationDataAccess.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for user ID: {UserId}", userId);
                    return null;
                }

                // Get application
                var application = await _applicationDataAccess.GetApplicationByIdAsync(id);
                if (application == null)
                {
                    _logger.LogInformation("Application not found with ID: {ApplicationId}", id);
                    return null;
                }

                // Authorization: Check if application belongs to student
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

        /// <summary>
        /// Create new application with validation and business logic
        /// </summary>
        /// <param name="createDto"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        // Auth: requires ApplicationsAdd permission (granted to Student, Staff, Manager, Admin).
        public async Task<ApplicationModel> CreateApplication(ApplicationModel application, string userId)
        {
            // Use FluentValidation to validate input
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

                // Process business rule here

                application.Id = string.Empty;
                application.DateApplied = DateTime.UtcNow;
                application.Status = "Pending"; // Default status

                var createdApplication = await _applicationDataAccess.CreateApplicationAsync(application);

                // Business logic: Return limited information
                var result = new ApplicationModel
                {
                    Id = createdApplication.Id,
                    Description = createdApplication.Description,
                    DateApplied = createdApplication.DateApplied,
                    Status = createdApplication.Status,
                    ApplicationType = createdApplication.ApplicationType
                };
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

        /// <summary>
        /// Update application status with validation and business logic
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        // Auth: requires ApplicationsEdit permission (staff-only — Student is never granted this).
        public async Task<bool> UpdateApplicationStatus(string id, string status, string userId)
        {
            try
            {
                var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
                if (!permissions.Contains(PermissionKey.ApplicationsEdit.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to update application status");
                }

                // Process business rule here

                if (!isAValidStatus(status))
                {
                    throw new ArgumentException("Status must be one of: Pending, Approved, Rejected, Studying");
                }

                var result = await _applicationDataAccess.UpdateApplicationStatusAsync(id, status);

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
            // "Studying" is set by EnrolmentService once staff enrol an accepted
            // application, not chosen directly through this status endpoint's normal
            // staff workflow, but it's still a legitimate value to accept/store here.
            return status == "Pending" || status == "Approved" || status == "Rejected" || status == "Studying";
        }

    }
}
