using Eduflex.DTOs.Address;
using Eduflex.DTOs.Student;
using Eduflex.Mapping.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class StudentsController : BaseApiController
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        // Self-service profile for the logged-in student — used to prefill things like
        // the application form's Applicant Details panel with basic account info,
        // without exposing the staff-facing Student search/management endpoints.
        // Hometown/current address and emergency contact live on the Application
        // itself (not here) since they can differ per application.
        [HttpGet("me")]
        public Task<ActionResult<StudentDto>> GetMyStudentProfile()
        {
            return HandleRequestAsync(_logger, "Error in GetMyStudentProfile endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var student = await _studentService.GetMyProfileAsync(userId)
                    ?? throw new KeyNotFoundException("Student profile not found");

                return new StudentDto
                {
                    Id = student.Id,
                    UserId = student.UserId,
                    Email = student.Email,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Nationality = student.Nationality,
                    PassportNumber = student.PassportNumber,
                    DateOfBirth = student.DateOfBirth,
                    PhoneNumber = student.PhoneNumber,
                    Address = student.Address == null ? new AddressDto() : new AddressDto
                    {
                        Street = student.Address.Street,
                        Suburb = student.Address.Suburb,
                        City = student.Address.City,
                        State = student.Address.State,
                        Country = student.Address.Country,
                        PostalCode = student.Address.PostalCode
                    }
                };
            });
        }

        [HttpPost("search-students")]
        public Task<ActionResult<PagedResult<StudentAccountDto>>> SearchStudents([FromBody] StudentFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search students endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var result = await _studentService.SearchStudentsAsync(filterDto.ToFilter(), actingUserId);

                return new PagedResult<StudentAccountDto>
                {
                    Items = result.Items.Select(s => s.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<StudentAccountDto>> GetStudent(string id)
        {
            return HandleRequestAsync(_logger, $"Error getting student: {id}", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var student = await _studentService.GetStudentAsync(id, actingUserId)
                    ?? throw new KeyNotFoundException("Student not found");

                return student.ToDto();
            });
        }

        [HttpPost("check-duplicate")]
        public Task<ActionResult<DuplicateCheckResultDto>> CheckDuplicate([FromBody] CheckDuplicateStudentDto checkDto)
        {
            return HandleRequestAsync(_logger, "Error in CheckDuplicate endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var result = await _studentService.CheckDuplicateAsync(
                    checkDto.Email, checkDto.Mobile, checkDto.DateOfBirth, checkDto.PassportNumber, actingUserId);

                return result.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<StudentAccountDto>> CreateStudent(CreateStudentDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateStudent endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var created = await _studentService.CreateStudentAsync(createDto.ToUserModel(), createDto.ToProfileModel(), actingUserId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<StudentAccountDto>> UpdateStudent(string id, UpdateStudentDto updateDto)
        {
            return HandleRequestAsync(_logger, $"Error updating student: {id}", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var updated = await _studentService.UpdateStudentAsync(id, updateDto.Email, updateDto.Mobile, updateDto.ToProfileModel(), actingUserId);
                return updated.ToDto();
            });
        }

        [HttpPost("{id}/deactivate")]
        public Task<ActionResult<bool>> DeactivateStudent(string id)
        {
            return HandleUpdateAsync(_logger, $"Error deactivating student: {id}", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _studentService.DeactivateStudentAsync(id, actingUserId);
            });
        }

        [HttpPost("{id}/reactivate")]
        public Task<ActionResult<bool>> ReactivateStudent(string id)
        {
            return HandleUpdateAsync(_logger, $"Error reactivating student: {id}", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _studentService.ReactivateStudentAsync(id, actingUserId);
            });
        }

        [HttpPost("{id}/send-reset-password")]
        public Task<ActionResult<bool>> SendResetPassword(string id)
        {
            return HandleUpdateAsync(_logger, $"Error sending password reset for student: {id}", async () =>
            {
                var actingUserId = GetRequiredUserId();
                await _studentService.SendPasswordResetAsync(id, actingUserId);
                return true;
            });
        }
    }
}
