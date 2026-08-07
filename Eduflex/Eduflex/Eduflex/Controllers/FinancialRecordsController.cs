using Eduflex.DTOs.Financial;
using Eduflex.Mapping.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Models.Enrolment;
using ShareService.Services.Interface;

namespace Eduflex.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "app")]
    public class FinancialRecordsController : BaseApiController
    {
        private readonly IFinancialRecordService _financialRecordService;
        // Data-access-level (not IEnrolmentService) and deliberately unchecked — same
        // "open, display-only bulk lookup" pattern as IEducationPartner.GetByIdsAsync /
        // IBusinessPartner.GetByIdsAsync. A finance user who already passed the
        // FinanceView check above shouldn't also need EnrolmentsView just to see the
        // student's name on the Finance list.
        private readonly IEnrolment _enrolmentDataAccess;
        private readonly ILogger<FinancialRecordsController> _logger;

        public FinancialRecordsController(IFinancialRecordService financialRecordService, IEnrolment enrolmentDataAccess, ILogger<FinancialRecordsController> logger)
        {
            _financialRecordService = financialRecordService;
            _enrolmentDataAccess = enrolmentDataAccess;
            _logger = logger;
        }

        private static string StudentNameOf(EnrolmentModel? enrolment) =>
            enrolment != null ? $"{enrolment.FirstName} {enrolment.LastName}".Trim() : string.Empty;

        [HttpGet("{id}")]
        public Task<ActionResult<FinancialRecordDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById financial record endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.GetByIdAsync(id, userId)
                    ?? throw new KeyNotFoundException("Financial record not found");
                var enrolment = await _enrolmentDataAccess.GetEnrolmentAsync(record.EnrolmentId);
                return record.ToDto(StudentNameOf(enrolment), enrolment?.Status ?? string.Empty);
            });
        }

        // No financial record exists until VISA success — a 404 here means "not created
        // yet", which the Enrolment detail page's "View Financial Record" button treats
        // as "don't show the button", not an error.
        [HttpGet("by-enrolment/{enrolmentId}")]
        public Task<ActionResult<FinancialRecordDto>> GetByEnrolmentId(string enrolmentId)
        {
            return HandleRequestAsync(_logger, "Error in GetByEnrolmentId financial record endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.GetByEnrolmentIdAsync(enrolmentId, userId)
                    ?? throw new KeyNotFoundException("No financial record exists yet for this enrolment");
                var enrolment = await _enrolmentDataAccess.GetEnrolmentAsync(record.EnrolmentId);
                return record.ToDto(StudentNameOf(enrolment), enrolment?.Status ?? string.Empty);
            });
        }

        [HttpPost("search-financial-records")]
        public Task<ActionResult<PagedResult<FinancialRecordDto>>> Search([FromBody] FinancialRecordFilterDto filterDto)
        {
            return HandleRequestAsync(_logger, "Error in Search financial records endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _financialRecordService.SearchAsync(filterDto.ToFilter(), userId);

                var enrolmentIds = result.Items.Select(r => r.EnrolmentId).Distinct();
                var enrolments = await _enrolmentDataAccess.GetByIdsAsync(enrolmentIds);
                var enrolmentById = enrolments.ToDictionary(e => e.Id);

                return new PagedResult<FinancialRecordDto>
                {
                    Items = result.Items.Select(r =>
                    {
                        enrolmentById.TryGetValue(r.EnrolmentId, out var enrolment);
                        return r.ToDto(StudentNameOf(enrolment), enrolment?.Status ?? string.Empty);
                    }).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        [HttpPost("{id}/commission-adjustments")]
        public Task<ActionResult<CommissionAdjustmentDto>> AddCommissionAdjustment(string id, AddCommissionAdjustmentDto addDto)
        {
            return HandleRequestAsync(_logger, "Error in AddCommissionAdjustment endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var adjustment = await _financialRecordService.AddCommissionAdjustmentAsync(id, addDto.Reason, addDto.Amount, userId);
                return adjustment.ToDto();
            });
        }

        [HttpPost("{id}/invoices")]
        public Task<ActionResult<InvoiceDto>> CreateInvoiceDraft(string id, CreateInvoiceDraftDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in CreateInvoiceDraft endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _financialRecordService.CreateInvoiceDraftAsync(
                    id, createDto.InvoiceNo, createDto.InvoiceToType, createDto.InvoiceToId, createDto.InvoiceToName, createDto.StudentName,
                    createDto.PeriodStart, createDto.PeriodEnd, createDto.PeriodTotal, createDto.HtmlContent, userId);
                return invoice.ToDto();
            });
        }

        [HttpPut("{id}/invoices/{invoiceId}")]
        public Task<ActionResult<bool>> UpdateInvoiceDraft(string id, string invoiceId, UpdateInvoiceDraftDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in UpdateInvoiceDraft endpoint", () =>
            {
                var userId = GetRequiredUserId();
                return _financialRecordService.UpdateInvoiceDraftAsync(id, invoiceId, updateDto.HtmlContent, updateDto.PeriodTotal, userId);
            });
        }

        [HttpPost("{id}/invoices/{invoiceId}/generate-pdf")]
        public Task<ActionResult<InvoiceDto>> GenerateInvoicePdf(string id, string invoiceId)
        {
            return HandleRequestAsync(_logger, "Error in GenerateInvoicePdf endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _financialRecordService.GenerateInvoicePdfAsync(id, invoiceId, userId);
                return invoice.ToDto();
            });
        }

        [HttpGet("{id}/invoices/{invoiceId}/download-link")]
        public Task<ActionResult<InvoiceDownloadLinkDto>> GetInvoiceDownloadLink(string id, string invoiceId)
        {
            return HandleRequestAsync(_logger, "Error in GetInvoiceDownloadLink endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var uri = await _financialRecordService.GetInvoiceDownloadLinkAsync(id, invoiceId, userId);
                return new InvoiceDownloadLinkDto { Url = uri.ToString() };
            });
        }

        [HttpPost("{id}/communications")]
        public Task<ActionResult<FinancialCommunicationDto>> SendCommunication(string id, SendFinancialCommunicationDto sendDto)
        {
            return HandleRequestAsync(_logger, "Error in SendCommunication endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var communication = await _financialRecordService.SendCommunicationAsync(
                    id, sendDto.ToEmail, sendDto.RecipientType, sendDto.Subject, sendDto.Body, sendDto.TemplateKey, sendDto.RelatedInvoiceId, userId);
                return communication.ToDto();
            });
        }

        private async Task<FinancialRecordDto> ToDtoWithEnrolmentAsync(ShareService.Models.Financial.FinancialRecordModel record)
        {
            var enrolment = await _enrolmentDataAccess.GetEnrolmentAsync(record.EnrolmentId);
            return record.ToDto(StudentNameOf(enrolment), enrolment?.Status ?? string.Empty);
        }

        [HttpPost("{id}/invoice-plan/regenerate")]
        public Task<ActionResult<FinancialRecordDto>> RegenerateInvoicePlan(string id)
        {
            return HandleRequestAsync(_logger, "Error in RegenerateInvoicePlan endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.RegenerateInvoicePlanAsync(id, userId);
                return await ToDtoWithEnrolmentAsync(record);
            });
        }

        [HttpPut("{id}/invoice-plan/{entryId}/date")]
        public Task<ActionResult<FinancialRecordDto>> UpdatePlanEntryDate(string id, string entryId, UpdatePlanEntryDateDto updateDto)
        {
            return HandleRequestAsync(_logger, "Error in UpdatePlanEntryDate endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.UpdatePlanEntryDateAsync(id, entryId, updateDto.ClaimDate, userId);
                return await ToDtoWithEnrolmentAsync(record);
            });
        }

        [HttpPost("{id}/invoice-plan/{entryId}/skip")]
        public Task<ActionResult<FinancialRecordDto>> SkipPlanEntry(string id, string entryId, SkipPlanEntryDto skipDto)
        {
            return HandleRequestAsync(_logger, "Error in SkipPlanEntry endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.SkipPlanEntryAsync(id, entryId, skipDto.Reason, userId);
                return await ToDtoWithEnrolmentAsync(record);
            });
        }

        [HttpPost("{id}/invoice-plan/{entryId}/restore")]
        public Task<ActionResult<FinancialRecordDto>> RestorePlanEntry(string id, string entryId)
        {
            return HandleRequestAsync(_logger, "Error in RestorePlanEntry endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.RestorePlanEntryAsync(id, entryId, userId);
                return await ToDtoWithEnrolmentAsync(record);
            });
        }

        [HttpPost("{id}/invoice-plan/manual")]
        public Task<ActionResult<FinancialRecordDto>> AddManualPlanEntry(string id, AddManualPlanEntryDto addDto)
        {
            return HandleRequestAsync(_logger, "Error in AddManualPlanEntry endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var record = await _financialRecordService.AddManualPlanEntryAsync(id, addDto.ClaimDate, userId);
                return await ToDtoWithEnrolmentAsync(record);
            });
        }
    }
}
