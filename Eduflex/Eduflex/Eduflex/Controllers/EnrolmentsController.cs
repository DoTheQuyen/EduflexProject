using Eduflex.Authorization;
using Eduflex.DTOs.Common;
using Eduflex.DTOs.DynamicForm;
using Eduflex.DTOs.Enrolment;
using Eduflex.Mapping.DynamicForm;
using Eduflex.Mapping.Enrolment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class EnrolmentsController : BaseApiController
    {
        private readonly IEnrolmentService _enrolmentService;
        private readonly IUserService _userService;
        private readonly ILogger<EnrolmentsController> _logger;

        public EnrolmentsController(IEnrolmentService enrolmentService, IUserService userService, ILogger<EnrolmentsController> logger)
        {
            _enrolmentService = enrolmentService;
            _userService = userService;
            _logger = logger;
        }

        private async Task<string> ResolveOwnerNameAsync(string? ownerUserId)
        {
            if (string.IsNullOrEmpty(ownerUserId)) return string.Empty;
            var user = await _userService.GetUserByIdAsync(ownerUserId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : ownerUserId;
        }

        [HttpPost("from-enquiry/{enquiryId}")]
        public Task<ActionResult<EnrolmentDto>> CreateFromEnquiry(string enquiryId, CreateEnrolmentDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateFromEnquiry endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var created = await _enrolmentService.CreateFromEnquiryAsync(enquiryId, createDto.ToModel(), createDto.StudentId, actingUserId);
                var ownerName = await ResolveOwnerNameAsync(created.OwnerUserId);
                return created.ToDto(ownerName);
            });
        }

        [HttpPost]
        public Task<ActionResult<EnrolmentDto>> CreateIndependent(CreateEnrolmentDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateIndependent endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var created = await _enrolmentService.CreateIndependentAsync(createDto.ToModel(), createDto.StudentId, actingUserId);
                var ownerName = await ResolveOwnerNameAsync(created.OwnerUserId);
                return created.ToDto(ownerName);
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<EnrolmentDto>> Get(string id)
        {
            return HandleRequestAsync(_logger, "Error in Get enrolment endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var enrolment = await _enrolmentService.GetEnrolmentAsync(id, actingUserId)
                    ?? throw new KeyNotFoundException("Enrolment not found");
                var ownerName = await ResolveOwnerNameAsync(enrolment.OwnerUserId);
                return enrolment.ToDto(ownerName);
            });
        }

        [HttpGet("enrolment-statuses")]
        [RequirePermission(PermissionKey.EnrolmentsView)]
        public Task<ActionResult<List<EnrolmentStatusDto>>> GetEnrolmentStatuses()
        {
            return HandleRequestAsync(_logger, "Error in GetEnrolmentStatuses endpoint", () =>
                Task.FromResult(Enum.GetValues<EnrolmentEnums>()
                    .Select(s => new EnrolmentStatusDto { Value = s.ToString(), Label = s.GetDescription() })
                    .ToList())
            );
        }

        [HttpPost("search-enrolments")]
        public Task<ActionResult<PagedResult<EnrolmentDto>>> SearchEnrolments([FromBody] EnrolmentFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search enrolments endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();

                var result = await _enrolmentService.GetEnrolmentsAsync(filterDto.ToFilter(actingUserId), actingUserId);

                var ownerNames = new Dictionary<string, string>();
                foreach (var ownerId in result.Items.Select(e => e.OwnerUserId).Distinct())
                {
                    ownerNames[ownerId] = await ResolveOwnerNameAsync(ownerId);
                }

                return new PagedResult<EnrolmentDto>
                {
                    Items = result.Items.Select(e => e.ToDto(ownerNames.GetValueOrDefault(e.OwnerUserId, e.OwnerUserId))).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> UpdateEnrolment(string id, EnrolmentDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateEnrolment endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.UpdateEnrolmentAsync(id, updateDto.ToModel(), actingUserId);
            });
        }

        [HttpPut("{id}/reassign")]
        public Task<ActionResult<bool>> Reassign(string id, ReassignEnrolmentDto reassignDto)
        {
            return HandleUpdateAsync(_logger, "Error in Reassign endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.ReassignOwnerAsync(id, reassignDto.NewOwnerUserId, actingUserId);
            });
        }

        [HttpPost("{id}/documents")]
        public Task<ActionResult<EnrolmentDocumentDto>> AddDocument(string id, AddEnrolmentDocumentDto documentDto)
        {
            return HandleRequestAsync(_logger, "Error in AddDocument endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var document = await _enrolmentService.AddDocumentAsync(id, documentDto.ToModel(), actingUserId);
                return new EnrolmentDocumentDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    Category = document.Category,
                    Url = document.Url,
                    ContentType = document.ContentType,
                    SizeBytes = document.SizeBytes,
                    UploadedByUserId = document.UploadedByUserId,
                    UploadedByName = document.UploadedByName,
                    IsFromStudent = document.IsFromStudent,
                    UploadedAt = document.UploadedAt
                };
            });
        }

        [HttpPut("{id}/documents/{documentId}")]
        public Task<ActionResult<bool>> RenameDocument(string id, string documentId, RenameEnrolmentDocumentDto renameDto)
        {
            return HandleUpdateAsync(_logger, "Error in RenameDocument endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.RenameDocumentAsync(id, documentId, renameDto.FileName, actingUserId);
            });
        }

        [HttpDelete("{id}/documents/{documentId}")]
        public Task<IActionResult> DeleteDocument(string id, string documentId)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteDocument endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.DeleteDocumentAsync(id, documentId, actingUserId);
            });
        }

        [HttpPut("{id}/visa-steps/{stepKey}")]
        public Task<ActionResult<bool>> SaveVisaStepDraft(string id, string stepKey, SaveVisaStepDto saveDto)
        {
            return HandleUpdateAsync(_logger, "Error in SaveVisaStepDraft endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.SaveVisaStepDraftAsync(id, stepKey, saveDto.Fields, actingUserId);
            });
        }

        [HttpPost("{id}/visa-steps/{stepKey}/complete")]
        public Task<ActionResult<bool>> CompleteVisaStep(string id, string stepKey, SaveVisaStepDto completeDto)
        {
            return HandleUpdateAsync(_logger, "Error in CompleteVisaStep endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.CompleteVisaStepAsync(id, stepKey, completeDto.Fields, actingUserId);
            });
        }

        [HttpPut("{id}/max-applications")]
        public Task<ActionResult<bool>> SetMaxApplicationsOverride(string id, SetMaxApplicationsOverrideDto maxDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetMaxApplicationsOverride endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.SetMaxApplicationsOverrideAsync(id, maxDto.MaxApplications, actingUserId);
            });
        }

        [HttpPost("{id}/course-applications")]
        public Task<ActionResult<CourseApplicationDto>> AddCourseApplication(string id, AddCourseApplicationDto addDto)
        {
            return HandleRequestAsync(_logger, "Error in AddCourseApplication endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var courseApplication = await _enrolmentService.AddCourseApplicationAsync(id, addDto.EducationPartnerId, addDto.CourseId, actingUserId);
                return courseApplication.ToDto();
            });
        }

        [HttpPut("{id}/course-applications/{courseApplicationId}/details")]
        public Task<ActionResult<bool>> UpdateCourseApplicationDetails(string id, string courseApplicationId, UpdateCourseApplicationDetailsDto detailsDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateCourseApplicationDetails endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.UpdateCourseApplicationDetailsAsync(id, courseApplicationId, detailsDto.ToModel(), actingUserId);
            });
        }

        [HttpPut("{id}/course-applications/{courseApplicationId}/status")]
        public Task<ActionResult<bool>> SetCourseApplicationStatus(string id, string courseApplicationId, SetCourseApplicationStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetCourseApplicationStatus endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.SetCourseApplicationStatusAsync(id, courseApplicationId, statusDto.Status, actingUserId);
            });
        }

        [HttpPost("{id}/course-applications/{courseApplicationId}/finalize")]
        public Task<ActionResult<bool>> FinalizeCourseApplication(string id, string courseApplicationId)
        {
            return HandleUpdateAsync(_logger, "Error in FinalizeCourseApplication endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.FinalizeCourseApplicationAsync(id, courseApplicationId, actingUserId);
            });
        }

        [HttpPost("{id}/communications")]
        public Task<ActionResult<EnrolmentCommunicationDto>> SendCommunication(string id, SendEnrolmentCommunicationDto sendDto)
        {
            return HandleRequestAsync(_logger, "Error in SendCommunication endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var communication = await _enrolmentService.SendCommunicationAsync(
                    id, sendDto.ToEmail, sendDto.RecipientType, sendDto.Subject, sendDto.Body, sendDto.TemplateKey, sendDto.AttachedDocumentIds, actingUserId);
                return new EnrolmentCommunicationDto
                {
                    Id = communication.Id,
                    TemplateKey = communication.TemplateKey,
                    ToEmail = communication.ToEmail,
                    RecipientType = communication.RecipientType,
                    Subject = communication.Subject,
                    Body = communication.Body,
                    AttachedDocumentIds = communication.AttachedDocumentIds,
                    SentByName = communication.SentByName,
                    SentAt = communication.SentAt
                };
            });
        }

        // ===================== Dynamic Forms — staff =====================

        [HttpPost("{id}/forms/request")]
        public Task<ActionResult<EnrolmentFormResponseDto>> RequestForm(string id, RequestFormDto requestDto)
        {
            return HandleRequestAsync(_logger, "Error in RequestForm endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var response = await _enrolmentService.RequestFormAsync(id, requestDto.FormTemplateId, actingUserId);
                return response.ToDto();
            });
        }

        [HttpPost("{id}/forms/{responseId}/withdraw")]
        public Task<ActionResult<bool>> WithdrawFormRequest(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in WithdrawFormRequest endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.WithdrawFormRequestAsync(id, responseId, actingUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/archive")]
        public Task<ActionResult<bool>> ArchiveFormResponse(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in ArchiveFormResponse endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.ArchiveFormResponseAsync(id, responseId, actingUserId);
            });
        }

        // Manual override for staff — bypasses the normal guarded transitions so a form
        // stuck in an unexpected state can be corrected directly.
        [HttpPut("{id}/forms/{responseId}/status")]
        public Task<ActionResult<bool>> SetFormResponseStatus(string id, string responseId, SetFormResponseStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetFormResponseStatus endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.SetFormResponseStatusAsync(id, responseId, statusDto.Status, actingUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/reopen")]
        public Task<ActionResult<bool>> ReopenFormForEdit(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in ReopenFormForEdit endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.ReopenFormForEditAsync(id, responseId, actingUserId);
            });
        }

        [HttpPut("{id}/forms/{responseId}/answers")]
        public Task<ActionResult<bool>> StaffEditFormResponse(string id, string responseId, SaveFormAnswersDto answersDto)
        {
            return HandleUpdateAsync(_logger, "Error in StaffEditFormResponse endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _enrolmentService.StaffEditFormResponseAsync(id, responseId, answersDto.ToModel(), actingUserId);
            });
        }

        // Streams the freshly-rendered PDF straight back with our own Content-Disposition
        // filename — see EnrolmentService.ExportFormAsync for why this doesn't go via blob
        // storage (Content-Disposition on a blob isn't reliably honored for inline PDFs).
        // Not routed through HandleRequestAsync since that wraps results in Ok(...), which
        // doesn't fit a file download.
        [HttpGet("{id}/forms/{responseId}/export")]
        public async Task<IActionResult> ExportForm(string id, string responseId)
        {
            try
            {
                var actingUserId = GetRequiredUserId();
                var (content, fileName) = await _enrolmentService.ExportFormAsync(id, responseId, actingUserId);
                return File(content, "application/pdf", fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExportForm endpoint");
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        // ===================== Dynamic Forms — student =====================

        // The Applications module only knows its own applicationId, not the linked
        // Enrolment's id — ownership is checked against StudentUserId inside the
        // service, not a permission key (a student may only ever act on their own
        // enrolment's forms).
        [HttpGet("by-application/{applicationId}/forms")]
        public Task<ActionResult<MyEnrolmentFormsDto>> GetMyForms(string applicationId)
        {
            return HandleRequestAsync(_logger, "Error in GetMyForms endpoint", async () =>
            {
                var studentUserId = GetRequiredUserId();
                var enrolment = await _enrolmentService.GetEnrolmentForStudentByApplicationIdAsync(applicationId, studentUserId)
                    ?? throw new KeyNotFoundException("No enrolment found for this application");

                return new MyEnrolmentFormsDto
                {
                    EnrolmentId = enrolment.Id,
                    Forms = enrolment.FormResponses
                        .Where(r => r.Status != "Withdrawn")
                        .OrderByDescending(r => r.RequestedAt)
                        .Select(r => r.ToDto())
                        .ToList()
                };
            });
        }

        [HttpPut("{id}/forms/{responseId}/draft")]
        public Task<ActionResult<bool>> SaveFormDraft(string id, string responseId, SaveFormAnswersDto answersDto)
        {
            return HandleUpdateAsync(_logger, "Error in SaveFormDraft endpoint", () =>
            {
                var studentUserId = GetRequiredUserId();
                return _enrolmentService.SaveFormDraftAsync(id, responseId, answersDto.ToModel(), studentUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/submit")]
        public Task<ActionResult<bool>> SubmitForm(string id, string responseId, SaveFormAnswersDto answersDto)
        {
            return HandleUpdateAsync(_logger, "Error in SubmitForm endpoint", () =>
            {
                var studentUserId = GetRequiredUserId();
                return _enrolmentService.SubmitFormAsync(id, responseId, answersDto.ToModel(), studentUserId);
            });
        }
    }
}
