using Eduflex.DTOs.Financial;
using Eduflex.DTOs.Invoice;
using Eduflex.Mapping.Invoice;
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
    public class InvoicesController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        // The full ledger view — lives in the same Admin area as Invoice Templates, so it
        // shares that permission gate (checked inside InvoiceService.GetAllAsync).
        // Paginated (default 50/page) — the ledger has no upper bound on invoice volume,
        // so returning the whole collection on every load doesn't scale.
        [HttpGet("all-invoices")]
        public Task<ActionResult<PagedResult<InvoiceRecordDto>>> GetAll(
            [FromQuery] string? category, [FromQuery] string? status,
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            return HandleRequestAsync(_logger, "Error in GetAll invoices endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var result = await _invoiceService.GetAllAsync(category, status, pageNumber, pageSize, userId);
                return new PagedResult<InvoiceRecordDto>
                {
                    Items = result.Items.Select(i => i.ToDto()).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize
                };
            });
        }

        // Used by the VISA Process tab to show whether this enrolment already has a sent
        // service-fee invoice — no separate permission gate, see InvoiceService for why.
        [HttpGet("by-enrolment/{enrolmentId}")]
        public Task<ActionResult<List<InvoiceRecordDto>>> GetByEnrolment(string enrolmentId)
        {
            return HandleRequestAsync(_logger, "Error in GetByEnrolment invoices endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoices = await _invoiceService.GetByEnrolmentIdAsync(enrolmentId, userId);
                return invoices.Select(i => i.ToDto()).ToList();
            });
        }

        // Used by the Finance module's Invoice tab to list every invoice sent for a
        // given commission record — same no-extra-gate reasoning as GetByEnrolment.
        [HttpGet("by-financial-record/{financialRecordId}")]
        public Task<ActionResult<List<InvoiceRecordDto>>> GetByFinancialRecord(string financialRecordId)
        {
            return HandleRequestAsync(_logger, "Error in GetByFinancialRecord invoices endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoices = await _invoiceService.GetByFinancialRecordIdAsync(financialRecordId, userId);
                return invoices.Select(i => i.ToDto()).ToList();
            });
        }

        [HttpPost("send")]
        public Task<ActionResult<InvoiceRecordDto>> Send(SendInvoiceDto sendDto)
        {
            return HandleRequestAsync(_logger, "Error in Send invoice endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _invoiceService.SendInvoiceAsync(sendDto.ToModel(), userId, GetActingRole());
                return invoice.ToDto();
            });
        }

        // Reuses Financial's InvoiceDownloadLinkDto ({ Url }) rather than declaring a
        // duplicate identically-shaped DTO under a different namespace.
        [HttpGet("{id}/download-link")]
        public Task<ActionResult<InvoiceDownloadLinkDto>> GetDownloadLink(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetDownloadLink invoice endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var uri = await _invoiceService.GetDownloadLinkAsync(id, userId);
                return new InvoiceDownloadLinkDto { Url = uri.ToString() };
            });
        }

        // Retries the notification email for an invoice whose send failed (status
        // "Failed") — the invoice and its PDF already exist, so this never re-reserves an
        // invoice number.
        [HttpPost("{id}/resend")]
        public Task<ActionResult<InvoiceRecordDto>> Resend(string id, ResendInvoiceDto resendDto)
        {
            return HandleRequestAsync(_logger, "Error in Resend invoice endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _invoiceService.ResendInvoiceAsync(id, resendDto.EmailSubject, resendDto.EmailBody, userId);
                return invoice.ToDto();
            });
        }

        // Soft-voids an invoice so it stops counting toward income, while keeping the
        // record and its number in the ledger for audit.
        [HttpPost("{id}/cancel")]
        public Task<ActionResult<InvoiceRecordDto>> Cancel(string id, CancelInvoiceDto cancelDto)
        {
            return HandleRequestAsync(_logger, "Error in Cancel invoice endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _invoiceService.CancelInvoiceAsync(id, cancelDto.Reason, userId);
                return invoice.ToDto();
            });
        }

        [HttpPost("{id}/confirm-payment")]
        public Task<ActionResult<InvoiceRecordDto>> ConfirmPayment(string id, ConfirmInvoicePaymentDto confirmDto)
        {
            return HandleRequestAsync(_logger, "Error in ConfirmPayment invoice endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoice = await _invoiceService.ConfirmPaymentAsync(id, confirmDto.PaymentEvidenceUrl, userId);
                return invoice.ToDto();
            });
        }
    }
}
