using Eduflex.DTOs.MigrationCase;
using Eduflex.Mapping.MigrationCase;
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
    public class MigrationCasesController : BaseApiController
    {
        private readonly IMigrationCaseService _caseService;
        private readonly ILogger<MigrationCasesController> _logger;

        public MigrationCasesController(IMigrationCaseService caseService, ILogger<MigrationCasesController> logger)
        {
            _caseService = caseService;
            _logger = logger;
        }

        [HttpPost("search")]
        public Task<ActionResult<PagedResult<MigrationCaseDto>>> Search([FromBody] MigrationCaseFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search migration cases endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var result = await _caseService.GetCasesAsync(filterDto.ToFilter(), actingUserId);

                return new PagedResult<MigrationCaseDto>
                {
                    Items = result.Items.Select(c => c.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<MigrationCaseDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById migration case endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var migrationCase = await _caseService.GetCaseAsync(id, actingUserId)
                    ?? throw new KeyNotFoundException("Migration case not found");
                return migrationCase.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<MigrationCaseDto>> Create(CreateMigrationCaseDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create migration case endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var created = await _caseService.CreateCaseAsync(
                    createDto.TemplateId, createDto.PrimaryContactName, createDto.PrimaryContactEmail,
                    createDto.PrimaryContactMobile, createDto.Notes, actingUserId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}/customer-info")]
        public Task<ActionResult<bool>> UpdateCustomerInfo(string id, UpdateCustomerInfoDto customerInfoDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateCustomerInfo endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.UpdateCustomerInfoAsync(id, customerInfoDto.ToModel(), actingUserId);
            });
        }

        [HttpPut("{id}/steps/{stepKey}")]
        public Task<ActionResult<bool>> SaveStepDraft(string id, string stepKey, SaveMigrationCaseStepDto saveDto)
        {
            return HandleUpdateAsync(_logger, "Error in SaveStepDraft endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.SaveStepDraftAsync(id, stepKey, saveDto.Fields, actingUserId);
            });
        }

        [HttpPost("{id}/steps/{stepKey}/complete")]
        public Task<ActionResult<bool>> CompleteStep(string id, string stepKey, SaveMigrationCaseStepDto completeDto)
        {
            return HandleUpdateAsync(_logger, "Error in CompleteStep endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.CompleteStepAsync(id, stepKey, completeDto.Fields, actingUserId);
            });
        }

        [HttpPost("{id}/steps/{stepKey}/reopen")]
        public Task<ActionResult<bool>> ReopenStep(string id, string stepKey)
        {
            return HandleUpdateAsync(_logger, "Error in ReopenStep endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.ReopenStepAsync(id, stepKey, actingUserId);
            });
        }

        [HttpPost("{id}/documents")]
        public Task<ActionResult<MigrationCaseDocumentDto>> AddDocument(string id, AddMigrationCaseDocumentDto documentDto)
        {
            return HandleRequestAsync(_logger, "Error in AddDocument endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var document = await _caseService.AddDocumentAsync(id, documentDto.ToModel(), actingUserId);
                return document.ToDto();
            });
        }

        [HttpDelete("{id}/documents/{documentId}")]
        public Task<IActionResult> DeleteDocument(string id, string documentId)
        {
            return HandleDeleteAsync(_logger, "Error in DeleteDocument endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.DeleteDocumentAsync(id, documentId, actingUserId);
            });
        }

        [HttpPost("{id}/reassign")]
        public Task<ActionResult<bool>> Reassign(string id, ReassignMigrationCaseDto reassignDto)
        {
            return HandleUpdateAsync(_logger, "Error in Reassign endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.ReassignOwnerAsync(id, reassignDto.NewOwnerUserId, actingUserId);
            });
        }

        [HttpPost("{id}/communications")]
        public Task<ActionResult<MigrationCaseCommunicationDto>> SendCommunication(string id, SendMigrationCaseCommunicationDto sendDto)
        {
            return HandleRequestAsync(_logger, "Error in SendCommunication endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var communication = await _caseService.SendCommunicationAsync(
                    id, sendDto.ToEmail, sendDto.RecipientType, sendDto.Subject, sendDto.Body, sendDto.TemplateKey, sendDto.AttachedDocumentIds, actingUserId);
                return communication.ToDto();
            });
        }

        // ===================== Dynamic Forms =====================

        [HttpPost("{id}/forms/request")]
        public Task<ActionResult<MigrationCaseFormResponseDto>> RequestForm(string id, RequestMigrationCaseFormDto requestDto)
        {
            return HandleRequestAsync(_logger, "Error in RequestForm endpoint", async () =>
            {
                var actingUserId = GetRequiredUserId();
                var response = await _caseService.RequestFormAsync(id, requestDto.FormTemplateId, actingUserId);
                return response.ToMigrationCaseDto();
            });
        }

        [HttpPut("{id}/forms/{responseId}/answers")]
        public Task<ActionResult<bool>> SaveFormAnswers(string id, string responseId, SaveMigrationCaseFormAnswersDto answersDto)
        {
            return HandleUpdateAsync(_logger, "Error in SaveFormAnswers endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.SaveFormAnswersAsync(id, responseId, answersDto.ToModel(), answersDto.MarkSubmitted, actingUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/withdraw")]
        public Task<ActionResult<bool>> WithdrawFormRequest(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in WithdrawFormRequest endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.WithdrawFormRequestAsync(id, responseId, actingUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/archive")]
        public Task<ActionResult<bool>> ArchiveFormResponse(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in ArchiveFormResponse endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.ArchiveFormResponseAsync(id, responseId, actingUserId);
            });
        }

        [HttpPut("{id}/forms/{responseId}/status")]
        public Task<ActionResult<bool>> SetFormResponseStatus(string id, string responseId, SetMigrationCaseFormStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetFormResponseStatus endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.SetFormResponseStatusAsync(id, responseId, statusDto.Status, actingUserId);
            });
        }

        [HttpPost("{id}/forms/{responseId}/reopen")]
        public Task<ActionResult<bool>> ReopenFormForEdit(string id, string responseId)
        {
            return HandleUpdateAsync(_logger, "Error in ReopenFormForEdit endpoint", () =>
            {
                var actingUserId = GetRequiredUserId();
                return _caseService.ReopenFormForEditAsync(id, responseId, actingUserId);
            });
        }

        // Not routed through HandleRequestAsync — see EnrolmentsController.ExportForm for
        // why a file download doesn't fit that Ok(...)-wrapping helper.
        [HttpGet("{id}/forms/{responseId}/export")]
        public async Task<IActionResult> ExportForm(string id, string responseId)
        {
            try
            {
                var actingUserId = GetRequiredUserId();
                var (content, fileName) = await _caseService.ExportFormAsync(id, responseId, actingUserId);
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
    }
}
