using Eduflex.Authorization;
using Eduflex.DTOs.Common;
using Eduflex.DTOs.Enrolment;
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
    }
}
