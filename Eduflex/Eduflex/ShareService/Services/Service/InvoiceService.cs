using Microsoft.Extensions.Options;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.Invoice;
using ShareService.Models.Setting;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoice _invoiceDataAccess;
        private readonly IInvoiceTemplate _invoiceTemplateDataAccess;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly IAzureEmailService _emailService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly IEnrolmentService _enrolmentService;
        private readonly int _documentLinkExpiryDays;

        public InvoiceService(
            IInvoice invoiceDataAccess,
            IInvoiceTemplate invoiceTemplateDataAccess,
            IUserService userService,
            IPermissionService permissionService,
            IAzureBlobDocStorageService blobStorageService,
            IAzureEmailService emailService,
            IInvoicePdfService invoicePdfService,
            IEnrolmentService enrolmentService,
            IOptions<DocumentLinkSettings> documentLinkSettings)
        {
            _invoiceDataAccess = invoiceDataAccess;
            _invoiceTemplateDataAccess = invoiceTemplateDataAccess;
            _userService = userService;
            _permissionService = permissionService;
            _blobStorageService = blobStorageService;
            _emailService = emailService;
            _invoicePdfService = invoicePdfService;
            _enrolmentService = enrolmentService;
            _documentLinkExpiryDays = documentLinkSettings.Value.ExpiryDays;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Unknown";
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId;
        }

        public async Task<List<InvoiceModel>> GetAllAsync(string? category, string? status, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.InvoiceTemplatesEdit, "view the invoice ledger");
            return await _invoiceDataAccess.GetAllAsync(category, status);
        }

        public async Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId)
        {
            // No separate permission gate — this is read alongside an enrolment the caller
            // already has ownership/permission access to (enforced by the enrolment lookup
            // that already happened before this is called from the VISA Process tab).
            return await _invoiceDataAccess.GetByEnrolmentIdAsync(enrolmentId);
        }

        public async Task<InvoiceModel> SendInvoiceAsync(SendInvoiceRequestModel request, string actingUserId, string? actingUserRole)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "send invoices");

            var template = await _invoiceTemplateDataAccess.GetByIdAsync(request.TemplateId)
                ?? throw new KeyNotFoundException("Invoice template not found");
            if (!template.IsActive)
            {
                throw new ArgumentException("This invoice template is inactive.");
            }

            // Description and GST are strictly config-driven once the template sets a
            // default — the server always wins over whatever the client sent, so there's
            // no way to smuggle a different value through even if the UI's read-only
            // fields were tampered with. Amount is the one field a Manager/Admin can
            // override; everyone else is held to the template's configured price.
            var description = !string.IsNullOrWhiteSpace(template.DefaultDescription) ? template.DefaultDescription! : request.Description;
            var gstRatePercent = template.DefaultGstRatePercent ?? request.GstRatePercent;

            var amount = request.Amount;
            if (template.DefaultAmount.HasValue)
            {
                var canOverrideAmount = actingUserRole == "Manager" || actingUserRole == "Admin";
                if (!canOverrideAmount || amount == default)
                {
                    amount = template.DefaultAmount.Value;
                }
            }

            var sequence = await _invoiceTemplateDataAccess.ReserveNextSequenceAsync(request.TemplateId)
                ?? throw new KeyNotFoundException("Invoice template not found");
            var invoiceNo = template.FormatInvoiceNo(sequence);

            request.Description = description;
            request.Amount = amount;
            request.GstRatePercent = gstRatePercent;

            var gstAmount = Math.Round(amount * gstRatePercent / 100m, 2);
            var total = amount + gstAmount;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var sentAt = DateTime.UtcNow;
            var html = BuildInvoiceHtml(template, invoiceNo, sentAt, request, gstAmount, total);

            var pdfBytes = await _invoicePdfService.RenderToPdfAsync(html);
            using var stream = new MemoryStream(pdfBytes);
            var fileName = BuildInvoiceFileName(invoiceNo, request.RecipientName, sentAt);
            var pdfUrl = await _blobStorageService.UploadAsync(stream, fileName, "application/pdf");

            var invoice = new InvoiceModel
            {
                InvoiceNo = invoiceNo,
                TemplateId = request.TemplateId,
                Category = template.Category,
                RecipientType = request.RecipientType,
                RecipientId = request.RecipientId,
                RecipientName = request.RecipientName,
                RecipientEmail = request.RecipientEmail,
                RelatedEnrolmentId = request.RelatedEnrolmentId,
                RelatedStepKey = request.RelatedStepKey,
                Description = request.Description,
                Amount = request.Amount,
                GstAmount = gstAmount,
                Total = total,
                HtmlContent = html,
                PdfUrl = pdfUrl,
                PdfFileName = fileName,
                Status = InvoiceStatuses.Sent,
                SentAt = sentAt,
                CreatedByUserId = actingUserId,
                CreatedByName = actingUserName
            };
            await _invoiceDataAccess.CreateAsync(invoice);

            var expiringUri = _blobStorageService.GetExpiringDownloadUri(pdfUrl, _documentLinkExpiryDays);

            var linkText = expiringUri.ToString();
            var hadLinkToken = request.EmailBody.Contains("{{invoiceLink}}");
            var subject = request.EmailSubject
                .Replace("{{invoiceNo}}", invoiceNo)
                .Replace("{{invoiceDescription}}", request.Description);
            var plainTextBody = request.EmailBody
                .Replace("{{invoiceNo}}", invoiceNo)
                .Replace("{{invoiceDescription}}", request.Description)
                .Replace("{{invoiceLink}}", linkText);
            var htmlBody = plainTextBody.Replace("\n", "<br/>");
            if (hadLinkToken)
            {
                htmlBody = htmlBody.Replace(linkText, $"<a href=\"{linkText}\">Download PDF</a>");
            }
            else
            {
                htmlBody += $"<p><strong>Invoice {invoiceNo}:</strong> <a href=\"{expiringUri}\">Download PDF</a> (link expires in {_documentLinkExpiryDays} day{(_documentLinkExpiryDays == 1 ? "" : "s")}).</p>";
            }
            await _emailService.SendEmailAsync(request.RecipientEmail, subject, htmlBody, plainTextBody);

            if (!string.IsNullOrEmpty(request.RelatedEnrolmentId) && !string.IsNullOrEmpty(request.RelatedStepKey))
            {
                await _enrolmentService.SetStepFieldsAsync(
                    request.RelatedEnrolmentId,
                    request.RelatedStepKey,
                    new Dictionary<string, string> { ["invoiceId"] = invoice.Id, ["invoiceSentAt"] = sentAt.ToString("O") },
                    $"Sent invoice \"{invoiceNo}\" to {request.RecipientName} ({request.RecipientEmail})",
                    actingUserId);
            }

            return invoice;
        }

        public async Task<Uri> GetDownloadLinkAsync(string invoiceId, string userId)
        {
            var invoice = await _invoiceDataAccess.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");
            if (string.IsNullOrEmpty(invoice.PdfUrl))
            {
                throw new ArgumentException("This invoice has no PDF on file.");
            }
            return _blobStorageService.GetExpiringDownloadUri(invoice.PdfUrl, _documentLinkExpiryDays);
        }

        public async Task<InvoiceModel> ConfirmPaymentAsync(string invoiceId, string? paymentEvidenceUrl, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "confirm invoice payments");

            var invoice = await _invoiceDataAccess.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");

            invoice.Status = InvoiceStatuses.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentEvidenceUrl = paymentEvidenceUrl;
            await _invoiceDataAccess.ReplaceAsync(invoiceId, invoice);

            if (!string.IsNullOrEmpty(invoice.RelatedEnrolmentId) && !string.IsNullOrEmpty(invoice.RelatedStepKey))
            {
                await _enrolmentService.SetStepFieldsAsync(
                    invoice.RelatedEnrolmentId,
                    invoice.RelatedStepKey,
                    new Dictionary<string, string> { ["invoicePaidAt"] = invoice.PaidAt.Value.ToString("O") },
                    $"Confirmed payment received for invoice \"{invoice.InvoiceNo}\"",
                    actingUserId);
            }

            return invoice;
        }

        // Generalized version of the reference partner-invoice PDF layout (FROM/TO blocks,
        // Item/Amount/GST/Subtotal table, Invoice Summary/Total, contact + bank boxes) —
        // driven by the admin-configured template's branding instead of being hardcoded
        // per-invoice like the old FinancialRecord invoice-tab's buildInvoiceTemplate().
        private static string BuildInvoiceHtml(InvoiceTemplateModel template, string invoiceNo, DateTime issuedAt,
            SendInvoiceRequestModel request, decimal gstAmount, decimal total)
        {
            var logoHtml = !string.IsNullOrEmpty(template.LogoUrl)
                ? $"<img src=\"{template.LogoUrl}\" style=\"max-height:72px;max-width:300px;\" />"
                : $"<h2 style=\"margin:0;color:#16233A;\">{template.SenderName}</h2>";

            var senderAddressHtml = string.Join("<br/>", template.SenderAddressLines.Select(l => System.Net.WebUtility.HtmlEncode(l)));
            var abnLine = !string.IsNullOrEmpty(template.SenderAbn) ? $"<p style=\"margin:0;\">ABN: {template.SenderAbn}</p>" : "";
            var phoneLine = !string.IsNullOrEmpty(template.SenderPhone) ? $"<p style=\"margin:0;\">M: {template.SenderPhone}</p>" : "";

            var bankBox = !string.IsNullOrEmpty(template.BankName)
                ? $@"<div style=""border:1px solid #DCE0E4;padding:12px 16px;border-radius:4px;"">
                        <p style=""margin:0 0 4px;font-weight:bold;"">Bank details:</p>
                        <p style=""margin:0;"">Bank Name: {template.BankName}</p>
                        <p style=""margin:0;"">BSB: {template.BankBsb}</p>
                        <p style=""margin:0;"">Account Number: {template.BankAccountNumber}</p>
                        <p style=""margin:0;"">Account Name: {template.BankAccountName}</p>
                    </div>"
                : "";

            return $@"
<div style=""font-family:Arial,Helvetica,sans-serif;color:#1B2430;"">
  <table width=""100%"" style=""margin-bottom:24px;"">
    <tr>
      <td style=""vertical-align:top;width:50%;"">{logoHtml}</td>
      <td style=""text-align:right;vertical-align:top;"">
        <h2 style=""margin:0 0 8px;"">Invoice</h2>
        <p style=""margin:0;font-size:14px;""><strong>Invoice No:</strong> {invoiceNo}</p>
        <p style=""margin:0;font-size:14px;""><strong>Invoice Date:</strong> {issuedAt:dd/MM/yyyy}</p>
      </td>
    </tr>
  </table>

  <table width=""100%"" style=""margin-bottom:24px;"">
    <tr>
      <td style=""vertical-align:top;width:50%;"">
        <p style=""margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;"">From</p>
        <p style=""margin:0;font-weight:bold;"">{template.SenderName}</p>
        <p style=""margin:0;"">{senderAddressHtml}</p>
        {abnLine}
        <p style=""margin:0;"">E: {template.SenderEmail}</p>
        {phoneLine}
      </td>
      <td style=""vertical-align:top;width:50%;text-align:right;"">
        <p style=""margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;"">To</p>
        <p style=""margin:0;font-weight:bold;"">{request.RecipientName}</p>
        <p style=""margin:0;"">{request.RecipientEmail}</p>
      </td>
    </tr>
  </table>

  <table width=""100%"" style=""border-collapse:collapse;margin-bottom:8px;"">
    <thead>
      <tr style=""background:#F5F6F7;"">
        <th style=""text-align:left;padding:8px;border:1px solid #DCE0E4;"">Item</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">Amount</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">GST ({request.GstRatePercent:0.##}%)</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">Subtotal</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td style=""padding:8px;border:1px solid #DCE0E4;"">{System.Net.WebUtility.HtmlEncode(request.Description)}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{request.Amount:N2}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{gstAmount:N2}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{total:N2}</td>
      </tr>
    </tbody>
    <tfoot>
      <tr>
        <td colspan=""3"" style=""padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;"">Total</td>
        <td style=""padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;"">{total:N2}</td>
      </tr>
    </tfoot>
  </table>

  <table width=""100%"" style=""margin-top:24px;"">
    <tr>
      <td style=""vertical-align:top;width:50%;"">
        <div style=""border:1px solid #DCE0E4;padding:12px 16px;border-radius:4px;"">
          <p style=""margin:0 0 4px;font-weight:bold;"">Any queries regarding this invoice please contact:</p>
          <p style=""margin:0;"">{template.SenderName}</p>
          <p style=""margin:0;"">E: {template.SenderEmail}</p>
          {phoneLine}
        </div>
      </td>
      <td style=""vertical-align:top;width:50%;padding-left:16px;"">{bankBox}</td>
    </tr>
  </table>
</div>".Trim();
        }

        private static string BuildInvoiceFileName(string invoiceNo, string recipientName, DateTime issuedAt)
        {
            static string Sanitize(string value)
            {
                var invalid = Path.GetInvalidFileNameChars();
                return new string(value.Where(c => !invalid.Contains(c)).ToArray()).Replace(" ", "-");
            }

            return $"{Sanitize(invoiceNo)}-{Sanitize(recipientName)}-{issuedAt:ddMMyyyy}.pdf";
        }
    }
}
