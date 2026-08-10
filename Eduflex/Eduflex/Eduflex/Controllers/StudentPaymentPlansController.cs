using Eduflex.DTOs.StudentPaymentPlan;
using Eduflex.Mapping.StudentPaymentPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class StudentPaymentPlansController : BaseApiController
    {
        private readonly IStudentPaymentPlanService _service;
        private readonly ILogger<StudentPaymentPlansController> _logger;

        public StudentPaymentPlansController(IStudentPaymentPlanService service, ILogger<StudentPaymentPlansController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("by-enrolment/{enrolmentId}")]
        public Task<ActionResult<List<StudentPaymentPlanEntryDto>>> GetByEnrolment(string enrolmentId)
        {
            return HandleRequestAsync(_logger, "Error in GetByEnrolment student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entries = await _service.GetByEnrolmentIdAsync(enrolmentId, userId);
                return entries.Select(e => e.ToDto()).ToList();
            });
        }

        [HttpPost("by-enrolment/{enrolmentId}/generate")]
        public Task<ActionResult<List<StudentPaymentPlanEntryDto>>> Generate(string enrolmentId, GenerateStudentPaymentPlanDto generateDto)
        {
            return HandleRequestAsync(_logger, "Error in Generate student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entries = await _service.GeneratePlanAsync(
                    enrolmentId, generateDto.StudentName, generateDto.CourseName,
                    generateDto.TotalAmount, generateDto.InstalmentCount, generateDto.FirstDueDate, generateDto.IntervalMonths, userId);
                return entries.Select(e => e.ToDto()).ToList();
            });
        }

        [HttpPost("by-enrolment/{enrolmentId}/manual")]
        public Task<ActionResult<StudentPaymentPlanEntryDto>> AddManual(string enrolmentId, AddManualStudentPlanEntryDto addDto)
        {
            return HandleRequestAsync(_logger, "Error in AddManual student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entry = await _service.AddManualEntryAsync(
                    enrolmentId, addDto.StudentName, addDto.CourseName, addDto.Label, addDto.Amount, addDto.DueDate, userId);
                return entry.ToDto();
            });
        }

        [HttpPut("{entryId}/date")]
        public Task<ActionResult<StudentPaymentPlanEntryDto>> UpdateDate(string entryId, UpdateStudentPlanEntryDateDto updateDto)
        {
            return HandleRequestAsync(_logger, "Error in UpdateDate student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entry = await _service.UpdateEntryDateAsync(entryId, updateDto.DueDate, userId);
                return entry.ToDto();
            });
        }

        [HttpPost("{entryId}/skip")]
        public Task<ActionResult<StudentPaymentPlanEntryDto>> Skip(string entryId, SkipStudentPlanEntryDto skipDto)
        {
            return HandleRequestAsync(_logger, "Error in Skip student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entry = await _service.SkipEntryAsync(entryId, skipDto.Reason, userId);
                return entry.ToDto();
            });
        }

        [HttpPost("{entryId}/restore")]
        public Task<ActionResult<StudentPaymentPlanEntryDto>> Restore(string entryId)
        {
            return HandleRequestAsync(_logger, "Error in Restore student payment plan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var entry = await _service.RestoreEntryAsync(entryId, userId);
                return entry.ToDto();
            });
        }
    }
}
