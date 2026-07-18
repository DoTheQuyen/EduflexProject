using FluentValidation;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ShareService.DataAccess.Interface;
using ShareService.Models.Application;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplication _applicationDataAccess;
        private readonly IValidator<CreateApplicationModel> _createApplicationValidator;
        private readonly ILogger<ApplicationService> _logger;
        private readonly IMongoClient _client;

        public ApplicationService(
            IApplication applicationDataAccess,
            IValidator<CreateApplicationModel> createApplicationValidator,
            ILogger<ApplicationService> logger,
            IMongoClient client)
        {
            _applicationDataAccess = applicationDataAccess;
            _createApplicationValidator = createApplicationValidator;
            _logger = logger;
            _client = client;
        }

        /// <summary>
        /// Get applications by student ID with business logic processing
        /// </summary>
        /// <param name="studentId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
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

        public async Task<List<ApplicationModel>> GetApplicationsByUserId(string userId)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("User ID cannot be empty");
                }

                // Authorization here (future implementation)

             
                var student = await _applicationDataAccess.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for user ID: {UserId}", userId);
                    return new List<ApplicationModel>();
                }

                // Process business rule here
                var applications = await _applicationDataAccess.GetApplicationsByStudentIdAsync(student.Id);

                // Business logic: Return limited information for list view
                var result = applications.Select(a => new ApplicationModel
                {
                    Id = a.Id,
                    Description = a.Description,
                    DateApplied = a.DateApplied,
                    Status = a.Status,
                    ApplicationType = a.ApplicationType
                }).ToList();

                _logger.LogInformation("Returned {Count} applications for user {UserId} (student: {StudentId})",
                    result.Count, userId, student.Id);

                return result;
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
        public async Task<ApplicationDetailModel?> GetApplicationById(string id, string userId)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("Application ID and User ID cannot be empty");
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
                    ApplicationType = application.ApplicationType
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
        public async Task<ApplicationModel> CreateApplication(CreateApplicationModel createDto)
        {            
            // Use FluentValidation to validate input
            var validate = await _createApplicationValidator.ValidateAsync(createDto);
            if (!validate.IsValid)
            {
                var errors = string.Join("; ", validate.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("Validation failed for application creation: {errors}", errors);
                throw new ArgumentException($"Validation failed: {errors}");
            }

            //practice transaction session
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();
            try
            {               

                // Authorization here (future implementation)

                // Process business rule here   

                var application = new ApplicationModel
                {
                    StudentId = createDto.StudentId,
                    StudentName = createDto.StudentName,
                    Description = createDto.Description,
                    Details = createDto.Details,
                    ApplicationType = createDto.ApplicationType,
                    DateApplied = DateTime.UtcNow,
                    Status = "Pending" // Default status
                };

                var createdApplication = await _applicationDataAccess.CreateApplicationAsync(application,session);

                // Business logic: Return limited information
                var result = new ApplicationModel
                {
                    Id = createdApplication.Id,
                    Description = createdApplication.Description,
                    DateApplied = createdApplication.DateApplied,
                    Status = createdApplication.Status,
                    ApplicationType = createdApplication.ApplicationType
                };
                await session.CommitTransactionAsync();
                _logger.LogInformation("Created new application with ID: {ApplicationId} for student {StudentId}",
                    result.Id, createDto.StudentId);

                return result;
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                _logger.LogError(ex, "Error in CreateApplication for student {StudentId}", createDto.StudentId);
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
        public async Task<bool> UpdateApplicationStatus(string id, string status)
        {
            //practice transaction session
            using var session = await _client.StartSessionAsync();
            session.StartTransaction();
            try
            {
                // Authorization here (future implementation)

                // Process business rule here

                if (!isAValidStatus(status))
                {
                    throw new ArgumentException("Status must be one of: Pending, Approved, Rejected");
                }

                var result = await _applicationDataAccess.UpdateApplicationStatusAsync(id, status);

                await session.CommitTransactionAsync();
                _logger.LogInformation("Status update for application {ApplicationId}: {Status} - Success: {Success}",
                    id, status, result);

                return result;
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                _logger.LogError(ex, "Error in UpdateApplicationStatus for application {ApplicationId}", id);
                throw new Exception("Error updating application status", ex);
            }
        }

        private bool isAValidStatus(string status)
        {
            return status == "Pending" || status == "Approved" || status == "Rejected";
        }

    }
}