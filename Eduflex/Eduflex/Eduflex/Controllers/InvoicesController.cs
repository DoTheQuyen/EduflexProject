using Eduflex.DTOs.Financial;
using Eduflex.DTOs.Invoice;
using Eduflex.Mapping.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [HttpGet]
        public Task<ActionResult<List<InvoiceRecordDto>>> GetAll([FromQuery] string? category, [FromQuery] string? status)
        {
            return HandleRequestAsync(_logger, "Error in GetAll invoices endpoint", async () =>
            {
                var userId = GetRequiredUserId();
                var invoices = await _invoiceService.GetAllAsync(category, status, userId);
                return invoices.Select(i => i.ToDto()).ToList();
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
