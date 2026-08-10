using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
// NB: ShareService.Models.Financial is deliberately NOT imported — it declares its own
// InvoiceModel (the legacy embedded one), which would make every InvoiceModel reference
// in this file ambiguous. The two types it needs are fully qualified below instead.
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
        // Data access rather than IFinancialRecordService — appending an audit/communication
        // entry for an invoice this service already authorized shouldn't re-run that
        // service's own permission gates, and going through the interface would couple the
        // two services in both directions.
        private readonly IFinancialRecord _financialRecordDataAccess;
        private readonly IStudentPaymentPlanService _studentPaymentPlanService;
        private readonly ILogger<InvoiceService> _logger;
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
            IFinancialRecord financialRecordDataAccess,
            IStudentPaymentPlanService studentPaymentPlanService,
            ILogger<InvoiceService> logger,
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
            _financialRecordDataAccess = financialRecordDataAccess;
            _studentPaymentPlanService = studentPaymentPlanService;
            _logger = logger;
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

        // Which permission gates an invoice action depends on who it's for, not which
        // screen it was triggered from: a Student invoice is an Enrolments-module action
        // (raised from the VISA Process step), while EducationPartner/BusinessPartner/
        // Custom invoices are a Finance-module action (raised from the Finance module's
        // Invoice tab or the admin Custom Send screen). Previously every action here
        // checked EnrolmentsEdit regardless of RecipientType, which let a user with
        // EnrolmentsEdit but no Finance access send/cancel/confirm partner commission
        // invoices — this routes each recipient type to the permission that actually
        // owns that workflow.
        private Task RequireInvoiceActionPermissionAsync(string recipientType, string userId, string action)
        {
            var key = recipientType == InvoiceRecipientTypes.Student
                ? PermissionKey.EnrolmentsEdit
                : PermissionKey.FinanceEdit;
            return RequirePermissionAsync(userId, key, action);
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Unknown";
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId;
        }

        public async Task<PagedResult<InvoiceModel>> GetAllAsync(string? category, string? status, int pageNumber, int pageSize, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.InvoiceTemplatesEdit, "view the invoice ledger");
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
            return await _invoiceDataAccess.GetAllAsync(category, status, pageNumber, pageSize);
        }

        public async Task<List<InvoiceModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId)
        {
            return await _invoiceDataAccess.GetByEnrolmentIdAsync(enrolmentId);
        }

        public async Task<List<InvoiceModel>> GetByFinancialRecordIdAsync(string financialRecordId, string userId)
        {
            // Same reasoning as GetByEnrolmentIdAsync above — read alongside a
            // FinancialRecord the caller already reached through the Finance module.
            return await _invoiceDataAccess.GetByFinancialRecordIdAsync(financialRecordId);
        }

        public async Task<InvoiceModel> SendInvoiceAsync(SendInvoiceRequestModel request, string actingUserId, string? actingUserRole)
        {
            await RequireInvoiceActionPermissionAsync(request.RecipientType, actingUserId, "send invoices");

            var template = await _invoiceTemplateDataAccess.GetByIdAsync(request.TemplateId)
                ?? throw new KeyNotFoundException("Invoice template not found");
            if (!template.IsActive)
            {
                throw new ArgumentException("This invoice template is inactive.");
            }

            if (template.Category == InvoiceTemplateCategories.Custom && string.IsNullOrWhiteSpace(request.CustomContent))
            {
                throw new ArgumentException("Custom invoices need invoice content.");
            }

            decimal amount;
            decimal gstRatePercent;
            decimal gstAmount;
            decimal total;

            if (request.ClaimItems.Count > 0)
            {
                // Partner multi-item claim — each item carries its own amount/GST, so the
                // template's DefaultDescription/DefaultAmount/DefaultGstRatePercent locking
                // (below) doesn't apply here; the set of items is inherently variable.
                amount = request.ClaimItems.Sum(i => i.Amount);
                gstAmount = request.ClaimItems.Sum(i => Math.Round(i.Amount * i.GstRatePercent / 100m, 2));
                total = amount + gstAmount;
                gstRatePercent = 0m;
                request.Description = string.Join("; ", request.ClaimItems.Select(i => i.Description));
            }
            else
            {
                // Description and GST are strictly config-driven once the template sets a
                // default — the server always wins over whatever the client sent, so there's
                // no way to smuggle a different value through even if the UI's read-only
                // fields were tampered with. Amount is the one field a Manager/Admin can
                // override; everyone else is held to the template's configured price.
                var description = !string.IsNullOrWhiteSpace(template.DefaultDescription) ? template.DefaultDescription! : request.Description;
                gstRatePercent = template.DefaultGstRatePercent ?? request.GstRatePercent;

                amount = request.Amount;
                if (template.DefaultAmount.HasValue)
                {
                    var canOverrideAmount = actingUserRole == "Manager" || actingUserRole == "Admin";
                    if (!canOverrideAmount || amount == default)
                    {
                        amount = template.DefaultAmount.Value;
                    }
                }

                gstAmount = Math.Round(amount * gstRatePercent / 100m, 2);
                total = amount + gstAmount;
                request.Description = description;
            }

            var sequence = await _invoiceTemplateDataAccess.ReserveNextSequenceAsync(request.TemplateId)
                ?? throw new KeyNotFoundException("Invoice template not found");
            var invoiceNo = template.FormatInvoiceNo(sequence);

            request.Amount = amount;
            request.GstRatePercent = gstRatePercent;

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
                RelatedFinancialRecordId = request.RelatedFinancialRecordId,
                Description = request.Description,
                Amount = request.Amount,
                GstAmount = gstAmount,
                Total = total,
                ClaimItems = request.ClaimItems,
                StudentName = request.StudentName,
                StudentRefCode = request.StudentRefCode,
                StudentEmail = request.StudentEmail,
                StudentPhone = request.StudentPhone,
                CourseName = request.CourseName,
                Campus = request.Campus,
                EducationPartnerName = request.EducationPartnerName,
                CustomContent = request.CustomContent,
                EmailSubject = request.EmailSubject,
                EmailBody = request.EmailBody,
                HtmlContent = html,
                PdfUrl = pdfUrl,
                PdfFileName = fileName,
                Status = InvoiceStatuses.Sent,
                SentAt = sentAt,
                CreatedByUserId = actingUserId,
                CreatedByName = actingUserName
            };
            await _invoiceDataAccess.CreateAsync(invoice);

            // The invoice + PDF are already persisted at this point, so an email failure
            // must NOT roll the whole request back — that would strand a reserved invoice
            // number with no record of it. Instead the invoice is flagged Failed and can be
            // retried with ResendInvoiceAsync.
            var emailError = await TrySendInvoiceEmailAsync(invoice);
            if (emailError != null)
            {
                invoice.Status = InvoiceStatuses.Failed;
                invoice.LastEmailError = emailError;
                await _invoiceDataAccess.ReplaceAsync(invoice.Id, invoice);
                await RecordFinancialActivityAsync(invoice, actingUserId, actingUserName,
                    $"Invoice \"{invoiceNo}\" created but the email to {invoice.RecipientEmail} FAILED: {emailError}", logCommunication: false);
                return invoice;
            }

            await RecordFinancialActivityAsync(invoice, actingUserId, actingUserName,
                $"Created and sent invoice \"{invoiceNo}\" ({invoice.Total:N2}) to {invoice.RecipientName} ({invoice.RecipientEmail})",
                logCommunication: true, planEntryId: request.RelatedInvoicePlanEntryId);

            if (!string.IsNullOrEmpty(request.RelatedStudentPlanEntryId))
            {
                await _studentPaymentPlanService.MarkEntryInvoicedAsync(request.RelatedStudentPlanEntryId, invoice.Id);
            }

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

        public async Task<InvoiceModel> ResendInvoiceAsync(string invoiceId, string? emailSubject, string? emailBody, string actingUserId)
        {
            var invoice = await _invoiceDataAccess.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");
            await RequireInvoiceActionPermissionAsync(invoice.RecipientType, actingUserId, "resend invoices");

            if (invoice.Status == InvoiceStatuses.Cancelled)
            {
                throw new ArgumentException("This invoice has been cancelled and can't be resent.");
            }
            if (string.IsNullOrEmpty(invoice.PdfUrl))
            {
                throw new ArgumentException("This invoice has no PDF on file, so it can't be resent.");
            }

            // Staff review/edit the message before resending, so whatever they send is
            // persisted back as this invoice's message — that also repairs invoices created
            // before the subject/body were stored at all, whose saved message is empty.
            if (!string.IsNullOrWhiteSpace(emailSubject)) { invoice.EmailSubject = emailSubject.Trim(); }
            if (!string.IsNullOrWhiteSpace(emailBody)) { invoice.EmailBody = emailBody.Trim(); }

            if (string.IsNullOrWhiteSpace(invoice.EmailSubject) || string.IsNullOrWhiteSpace(invoice.EmailBody))
            {
                throw new ArgumentException("Give this invoice an email subject and message before resending.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var emailError = await TrySendInvoiceEmailAsync(invoice);
            if (emailError != null)
            {
                invoice.Status = InvoiceStatuses.Failed;
                invoice.LastEmailError = emailError;
                await _invoiceDataAccess.ReplaceAsync(invoice.Id, invoice);
                await RecordFinancialActivityAsync(invoice, actingUserId, actingUserName,
                    $"Resend of invoice \"{invoice.InvoiceNo}\" to {invoice.RecipientEmail} FAILED: {emailError}", logCommunication: false);
                throw new ArgumentException($"Could not send the email: {emailError}");
            }

            // A successful resend clears the failure and puts the invoice back in the
            // normal Sent state — Paid is left alone, since resending a receipt copy of an
            // already-paid invoice shouldn't reopen it.
            if (invoice.Status != InvoiceStatuses.Paid)
            {
                invoice.Status = InvoiceStatuses.Sent;
            }
            invoice.LastEmailError = null;
            await _invoiceDataAccess.ReplaceAsync(invoice.Id, invoice);

            await RecordFinancialActivityAsync(invoice, actingUserId, actingUserName,
                $"Resent invoice \"{invoice.InvoiceNo}\" to {invoice.RecipientName} ({invoice.RecipientEmail})", logCommunication: true);

            return invoice;
        }

        public async Task<InvoiceModel> CancelInvoiceAsync(string invoiceId, string? reason, string actingUserId)
        {
            var invoice = await _invoiceDataAccess.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");
            await RequireInvoiceActionPermissionAsync(invoice.RecipientType, actingUserId, "cancel invoices");

            if (invoice.Status == InvoiceStatuses.Cancelled)
            {
                throw new ArgumentException("This invoice is already cancelled.");
            }
            if (invoice.Status == InvoiceStatuses.Paid)
            {
                throw new ArgumentException("This invoice is already marked paid — cancel it only after reversing the payment.");
            }

            // Soft void: the document and its number stay in the ledger for audit, but
            // InvoiceStatuses.CountsAsIncome() now excludes it from revenue totals.
            invoice.Status = InvoiceStatuses.Cancelled;
            invoice.CancelledAt = DateTime.UtcNow;
            invoice.CancelReason = reason;
            await _invoiceDataAccess.ReplaceAsync(invoice.Id, invoice);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var reasonSuffix = string.IsNullOrWhiteSpace(reason) ? "" : $" — reason: {reason}";
            await RecordFinancialActivityAsync(invoice, actingUserId, actingUserName,
                $"Cancelled invoice \"{invoice.InvoiceNo}\" ({invoice.Total:N2}){reasonSuffix}", logCommunication: false);

            return invoice;
        }

        // Returns null on success, or the error message on failure — deliberately doesn't
        // throw, so callers decide whether a failed email should fail the whole operation.
        private async Task<string?> TrySendInvoiceEmailAsync(InvoiceModel invoice)
        {
            try
            {
                var expiringUri = _blobStorageService.GetExpiringDownloadUri(invoice.PdfUrl!, _documentLinkExpiryDays);
                var linkText = expiringUri.ToString();
                var hadLinkToken = invoice.EmailBody.Contains("{{invoiceLink}}");

                var subject = invoice.EmailSubject
                    .Replace("{{invoiceNo}}", invoice.InvoiceNo)
                    .Replace("{{invoiceDescription}}", invoice.Description);
                var plainTextBody = invoice.EmailBody
                    .Replace("{{invoiceNo}}", invoice.InvoiceNo)
                    .Replace("{{invoiceDescription}}", invoice.Description)
                    .Replace("{{invoiceLink}}", linkText);

                var htmlBody = plainTextBody.Replace("\n", "<br/>");
                if (hadLinkToken)
                {
                    htmlBody = htmlBody.Replace(linkText, $"<a href=\"{linkText}\">Download PDF</a>");
                }
                else
                {
                    htmlBody += $"<p><strong>Invoice {invoice.InvoiceNo}:</strong> <a href=\"{expiringUri}\">Download PDF</a> (link expires in {_documentLinkExpiryDays} day{(_documentLinkExpiryDays == 1 ? "" : "s")}).</p>";
                }

                await _emailService.SendEmailAsync(invoice.RecipientEmail, subject, htmlBody, plainTextBody);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice email for invoice {InvoiceNo}", invoice.InvoiceNo);
                return ex.Message;
            }
        }

        // Writes the invoice event onto the linked FinancialRecord's audit trail (and, for
        // emails that actually went out, its Communications list) so the Finance module's
        // Audit Trail and Communication tabs reflect invoice activity. No-ops for invoices
        // raised outside a financial record, e.g. student service-fee invoices, whose
        // history lives on the Enrolment instead.
        private async Task RecordFinancialActivityAsync(InvoiceModel invoice, string actingUserId, string actingUserName,
            string auditDescription, bool logCommunication, string? planEntryId = null)
        {
            if (string.IsNullOrEmpty(invoice.RelatedFinancialRecordId)) return;

            var record = await _financialRecordDataAccess.GetByIdAsync(invoice.RelatedFinancialRecordId);
            if (record == null) return;

            record.AuditTrail.Add(ShareService.Models.Financial.FinancialAuditEntryModel.Create(auditDescription, actingUserId, actingUserName));

            // Marks the specific claim on the invoice request calendar this invoice was
            // raised from as fulfilled, so the Finance tab shows it linked without a
            // separate call — see FinanceTabComponent's "Send Invoice" action per claim.
            if (!string.IsNullOrEmpty(planEntryId))
            {
                var planEntry = record.InvoicePlan.FirstOrDefault(p => p.Id == planEntryId);
                if (planEntry != null)
                {
                    planEntry.Status = "Invoiced";
                    planEntry.LinkedInvoiceId = invoice.Id;
                }
            }

            if (logCommunication)
            {
                record.Communications.Add(new ShareService.Models.Financial.FinancialCommunicationModel
                {
                    ToEmail = invoice.RecipientEmail,
                    RecipientType = invoice.RecipientType,
                    Subject = invoice.EmailSubject.Replace("{{invoiceNo}}", invoice.InvoiceNo),
                    Body = invoice.EmailBody
                        .Replace("{{invoiceNo}}", invoice.InvoiceNo)
                        .Replace("{{invoiceDescription}}", invoice.Description),
                    RelatedInvoiceId = invoice.Id,
                    SentByUserId = actingUserId,
                    SentByName = actingUserName,
                    SentAt = DateTime.UtcNow
                });
            }

            await _financialRecordDataAccess.ReplaceAsync(record.Id, record);
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
            var invoice = await _invoiceDataAccess.GetByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");
            await RequireInvoiceActionPermissionAsync(invoice.RecipientType, actingUserId, "confirm invoice payments");

            if (invoice.Status == InvoiceStatuses.Cancelled)
            {
                throw new ArgumentException("This invoice has been cancelled — reinstate it before recording a payment.");
            }

            invoice.Status = InvoiceStatuses.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentEvidenceUrl = paymentEvidenceUrl;
            await _invoiceDataAccess.ReplaceAsync(invoiceId, invoice);

            var payAuditUserName = await ResolveUserNameAsync(actingUserId);
            await RecordFinancialActivityAsync(invoice, actingUserId, payAuditUserName,
                $"Confirmed payment received for invoice \"{invoice.InvoiceNo}\" ({invoice.Total:N2})", logCommunication: false);

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

            var studentBlock = BuildStudentBlockHtml(request);
            var bodyHtml = template.Category == InvoiceTemplateCategories.Custom
                ? BuildFreeTextBodyHtml(request.CustomContent)
                : BuildClaimItemsHtml(request, gstAmount, total);

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

  {studentBlock}

  {bodyHtml}

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

        // Partner invoices only, and only when the caller actually supplied a linked
        // Enrolment's details (see InvoiceService.SendInvoiceAsync callers) — omitted
        // entirely otherwise, so Customer/Custom invoices and Partner invoices raised
        // outside an enrolment context render exactly as before.
        private static string BuildStudentBlockHtml(SendInvoiceRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentName)) return "";

            static string Cell(string label, string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "";
                return $@"<td style=""padding:2px 20px 6px 0;vertical-align:top;"">
                    <p style=""margin:0;color:#5C6B7A;font-size:11px;text-transform:uppercase;"">{label}</p>
                    <p style=""margin:0;font-weight:bold;font-size:13px;"">{System.Net.WebUtility.HtmlEncode(value)}</p>
                  </td>";
            }

            return $@"<div style=""border:1.5px dashed #B8862F;border-radius:4px;padding:12px 16px;margin-bottom:24px;"">
    <p style=""margin:0 0 8px;color:#5C6B7A;font-size:11px;text-transform:uppercase;"">Student &amp; Enrolment</p>
    <table>
      <tr>
        {Cell("Student", request.StudentName)}
        {Cell("Student ID", request.StudentRefCode)}
        {Cell("Course", request.CourseName)}
        {Cell("Campus", request.Campus)}
      </tr>
      <tr>
        {Cell("Uni", request.EducationPartnerName)}
        {Cell("Email", request.StudentEmail)}
        {Cell("Phone", request.StudentPhone)}
      </tr>
    </table>
  </div>";
        }

        // Renders as many rows as there are claim items (Partner invoices with multiple
        // items) or exactly one row from the legacy single Description/Amount fields
        // (Customer invoices, and Partner invoices with no items list) — the two shapes
        // converge into the same table markup either way.
        private static string BuildClaimItemsHtml(SendInvoiceRequestModel request, decimal gstAmount, decimal total)
        {
            string Row(string description, decimal amount, decimal itemGst) => $@"<tr>
        <td style=""padding:8px;border:1px solid #DCE0E4;"">{System.Net.WebUtility.HtmlEncode(description)}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{amount:N2}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{itemGst:N2}</td>
        <td style=""padding:8px;border:1px solid #DCE0E4;text-align:right;"">{(amount + itemGst):N2}</td>
      </tr>";

            var rowsHtml = request.ClaimItems.Count > 0
                ? string.Join("", request.ClaimItems.Select(i => Row(i.Description, i.Amount, Math.Round(i.Amount * i.GstRatePercent / 100m, 2))))
                : Row(request.Description, request.Amount, gstAmount);

            return $@"<table width=""100%"" style=""border-collapse:collapse;margin-bottom:8px;"">
    <thead>
      <tr style=""background:#F5F6F7;"">
        <th style=""text-align:left;padding:8px;border:1px solid #DCE0E4;"">Item</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">Amount</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">GST</th>
        <th style=""text-align:right;padding:8px;border:1px solid #DCE0E4;"">Subtotal</th>
      </tr>
    </thead>
    <tbody>
      {rowsHtml}
    </tbody>
    <tfoot>
      <tr>
        <td colspan=""3"" style=""padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;"">Total</td>
        <td style=""padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;"">{total:N2}</td>
      </tr>
    </tfoot>
  </table>";
        }

        // Custom category only — staff-authored free text in place of the claims table,
        // blank-line-separated into paragraphs the same way SendInvoiceAsync's plain-text
        // email body becomes HTML (single "\n" -> <br/>, blank line -> new paragraph).
        private static string BuildFreeTextBodyHtml(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "";

            var paragraphs = content.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(p => System.Net.WebUtility.HtmlEncode(p.Trim()).Replace("\n", "<br/>"))
                .Select(p => $@"<p style=""margin:0 0 12px;"">{p}</p>");

            return $@"<div style=""font-size:13px;line-height:1.7;border-bottom:1px solid #DCE0E4;padding-bottom:16px;margin-bottom:24px;"">
    {string.Join("", paragraphs)}
  </div>";
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
