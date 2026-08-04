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
    public class InvoiceTemplatesController : BaseApiController
    {
        private readonly IInvoiceTemplateService _invoiceTemplateService;
        private readonly ILogger<InvoiceTemplatesController> _logger;

        public InvoiceTemplatesController(IInvoiceTemplateService invoiceTemplateService, ILogger<InvoiceTemplatesController> logger)
        {
            _invoiceTemplateService = invoiceTemplateService;
            _logger = logger;
        }

        // Ungated — any staff member sending an invoice needs this list to populate their
        // template picker. The gated GetById/Create/Update/SetStatus below are the admin
        // management screen. Same reasoning as EmailTemplatesController.GetAll.
        [HttpGet]
        public Task<ActionResult<List<InvoiceTemplateDto>>> GetAll()
        {
            return HandleRequestAsync(_logger, "Error in GetAll invoice templates endpoint", async () =>
            {
                var templates = await _invoiceTemplateService.GetAllAsync();
                return templates.Select(t => t.ToDto()).ToList();
            });
        }

        [HttpGet("{id}")]
        public Task<ActionResult<InvoiceTemplateDto>> GetById(string id)
        {
            return HandleRequestAsync(_logger, "Error in GetById invoice template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var template = await _invoiceTemplateService.GetByIdAsync(id, userId)
                    ?? throw new KeyNotFoundException("Invoice template not found");
                return template.ToDto();
            });
        }

        [HttpPost]
        public Task<ActionResult<InvoiceTemplateDto>> Create(CreateInvoiceTemplateDto createDto)
        {
            return HandleRequestAsync(_logger, "Error in Create invoice template endpoint", async () =>
            {
                var userId = GetRequiredUserId();

                var created = await _invoiceTemplateService.CreateAsync(createDto.ToModel(), userId);
                return created.ToDto();
            });
        }

        [HttpPut("{id}")]
        public Task<ActionResult<bool>> Update(string id, UpdateInvoiceTemplateDto updateDto)
        {
            return HandleUpdateAsync(_logger, "Error in Update invoice template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _invoiceTemplateService.UpdateAsync(id, updateDto.ToModel(), userId);
            });
        }

        [HttpPost("{id}/status")]
        public Task<ActionResult<bool>> SetStatus(string id, SetInvoiceTemplateStatusDto statusDto)
        {
            return HandleUpdateAsync(_logger, "Error in SetStatus invoice template endpoint", () =>
            {
                var userId = GetRequiredUserId();

                return _invoiceTemplateService.SetStatusAsync(id, statusDto.IsActive, userId);
            });
        }
    }
}
