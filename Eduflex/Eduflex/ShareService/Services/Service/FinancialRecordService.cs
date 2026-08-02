using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Enrolment;
using ShareService.Models.Financial;
using ShareService.Models.Setting;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class FinancialRecordService : IFinancialRecordService
    {
        private readonly IFinancialRecord _financialRecordDataAccess;
        private readonly ICourse _courseDataAccess;
        private readonly IBusinessPartner _businessPartnerDataAccess;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly IAzureEmailService _emailService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger<FinancialRecordService> _logger;
        private readonly int _documentLinkExpiryDays;

        public FinancialRecordService(
            IFinancialRecord financialRecordDataAccess,
            ICourse courseDataAccess,
            IBusinessPartner businessPartnerDataAccess,
            IUserService userService,
            IPermissionService permissionService,
            IAzureBlobDocStorageService blobStorageService,
            IAzureEmailService emailService,
            IInvoicePdfService invoicePdfService,
            INotificationPublisher notificationPublisher,
            IOptions<DocumentLinkSettings> documentLinkSettings,
            ILogger<FinancialRecordService> logger)
        {
            _financialRecordDataAccess = financialRecordDataAccess;
            _courseDataAccess = courseDataAccess;
            _businessPartnerDataAccess = businessPartnerDataAccess;
            _userService = userService;
            _permissionService = permissionService;
            _blobStorageService = blobStorageService;
            _emailService = emailService;
            _invoicePdfService = invoicePdfService;
            _notificationPublisher = notificationPublisher;
            _documentLinkExpiryDays = documentLinkSettings.Value.ExpiryDays;
            _logger = logger;
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

        public async Task<FinancialRecordModel> CreateForEnrolmentIfNotExistsAsync(EnrolmentModel enrolment, string actingUserId)
        {
            var existing = await _financialRecordDataAccess.GetByEnrolmentIdAsync(enrolment.Id);
            if (existing != null)
            {
                return existing;
            }

            var course = !string.IsNullOrEmpty(enrolment.CourseId)
                ? await _courseDataAccess.GetCourseByIdAsync(enrolment.CourseId)
                : null;
            var courseCommissionRate = course?.CommissionBaseRate ?? 0;
            var totalTuition = enrolment.TuitionFee ?? course?.TuitionFee ?? 0;

            var businessPartner = !string.IsNullOrEmpty(enrolment.BusinessPartnerId)
                ? await _businessPartnerDataAccess.GetBusinessPartnerByIdAsync(enrolment.BusinessPartnerId)
                : null;
            var businessPartnerCommissionRate = businessPartner?.CommissionBaseRate ?? 0;

            var expectedCommission = businessPartner != null
                ? (courseCommissionRate / 100m) * (businessPartnerCommissionRate / 100m) * totalTuition
                : (courseCommissionRate / 100m) * totalTuition;

            var invoicePlan = new List<InvoicePlanEntryModel>();
            if (enrolment.ActualCommencementDate.HasValue)
            {
                invoicePlan.Add(new InvoicePlanEntryModel
                {
                    PlannedRequestDate = enrolment.ActualCommencementDate.Value.AddMonths(1),
                    Status = "Planned"
                });
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var record = new FinancialRecordModel
            {
                EnrolmentId = enrolment.Id,
                EducationPartnerId = enrolment.EducationPartnerId,
                BusinessPartnerId = enrolment.BusinessPartnerId,
                CourseCommissionRate = courseCommissionRate,
                BusinessPartnerCommissionRate = businessPartnerCommissionRate,
                TotalTuition = totalTuition,
                ExpectedCommission = expectedCommission,
                InvoicePlan = invoicePlan,
                AuditTrail = new List<FinancialAuditEntryModel>
                {
                    FinancialAuditEntryModel.Create("Financial record auto-created on VISA success", actingUserId, actingUserName)
                }
            };

            await _financialRecordDataAccess.CreateAsync(record);

            await _notificationPublisher.PublishToRoleAsync(
                module: "Finance",
                entityId: record.Id,
                summary: $"Financial record created for enrolment {enrolment.Id}",
                role: SystemRole.Manager);

            _logger.LogInformation("Created financial record {FinancialRecordId} for enrolment {EnrolmentId}", record.Id, enrolment.Id);
            return record;
        }

        public async Task<FinancialRecordModel?> GetByIdAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view financial records");
            return await _financialRecordDataAccess.GetByIdAsync(id);
        }

        public async Task<FinancialRecordModel?> GetByEnrolmentIdAsync(string enrolmentId, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view financial records");
            return await _financialRecordDataAccess.GetByEnrolmentIdAsync(enrolmentId);
        }

        public async Task<PagedResult<FinancialRecordModel>> SearchAsync(FinancialRecordFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view financial records");
            return await _financialRecordDataAccess.GetFinancialRecordsAsync(filter);
        }

        public async Task<CommissionAdjustmentModel> AddCommissionAdjustmentAsync(string id, string reason, decimal amount, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit financial records");
            var existing = await GetExistingAsync(id);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var adjustment = new CommissionAdjustmentModel
            {
                Reason = reason,
                Amount = amount,
                AddedByUserId = actingUserId,
                AddedByName = actingUserName
            };

            existing.ExtraCommissionAdjustments.Add(adjustment);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Added commission adjustment \"{reason}\" ({amount:+0.00;-0.00})", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return adjustment;
        }

        public async Task<InvoiceModel> CreateInvoiceDraftAsync(string id, string invoiceNo, string invoiceToType, string invoiceToId, string invoiceToName, string studentName,
            DateTime periodStart, DateTime periodEnd, decimal periodTotal, string htmlContent, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceAdd, "add invoices");
            var existing = await GetExistingAsync(id);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var invoice = new InvoiceModel
            {
                InvoiceNo = invoiceNo,
                InvoiceToType = invoiceToType,
                InvoiceToId = invoiceToId,
                InvoiceToName = invoiceToName,
                StudentName = studentName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PeriodTotal = periodTotal,
                HtmlContent = htmlContent,
                Status = "Draft",
                CreatedByUserId = actingUserId,
                CreatedByName = actingUserName
            };

            existing.Invoices.Add(invoice);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Created invoice draft \"{invoiceNo}\"", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);

            await _notificationPublisher.PublishToRoleAsync(
                module: "Finance",
                entityId: id,
                summary: $"Invoice draft \"{invoiceNo}\" created",
                role: SystemRole.Manager);

            return invoice;
        }

        public async Task<bool> UpdateInvoiceDraftAsync(string id, string invoiceId, string htmlContent, decimal periodTotal, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit invoices");
            var existing = await GetExistingAsync(id);
            var invoice = FindInvoice(existing, invoiceId);

            if (invoice.Status != "Draft")
            {
                throw new ArgumentException("This invoice has already been generated and can no longer be edited as a draft.");
            }

            invoice.HtmlContent = htmlContent;
            invoice.PeriodTotal = periodTotal;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Saved draft changes to invoice \"{invoice.InvoiceNo}\"", actingUserId, actingUserName));

            return await _financialRecordDataAccess.ReplaceAsync(id, existing);
        }

        public async Task<InvoiceModel> GenerateInvoicePdfAsync(string id, string invoiceId, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "generate invoice PDFs");
            var existing = await GetExistingAsync(id);
            var invoice = FindInvoice(existing, invoiceId);

            var fileName = BuildInvoiceFileName(invoice.InvoiceNo, invoice.InvoiceToName, invoice.StudentName, DateTime.UtcNow);

            var pdfBytes = await _invoicePdfService.RenderToPdfAsync(invoice.HtmlContent);
            using var stream = new MemoryStream(pdfBytes);
            var pdfUrl = await _blobStorageService.UploadAsync(stream, fileName, "application/pdf");

            invoice.PdfUrl = pdfUrl;
            invoice.PdfFileName = fileName;
            invoice.Status = "Generated";
            invoice.GeneratedAt = DateTime.UtcNow;

            var planEntry = existing.InvoicePlan.FirstOrDefault(p => p.Status == "Planned");
            if (planEntry != null)
            {
                planEntry.Status = "Invoiced";
                planEntry.LinkedInvoiceId = invoice.Id;
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Generated PDF for invoice \"{invoice.InvoiceNo}\"", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);

            await _notificationPublisher.PublishToRoleAsync(
                module: "Finance",
                entityId: id,
                summary: $"Invoice \"{invoice.InvoiceNo}\" status changed to Generated",
                role: SystemRole.Manager);

            return invoice;
        }

        public async Task<Uri> GetInvoiceDownloadLinkAsync(string id, string invoiceId, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.FinanceView, "view financial records");
            var existing = await GetExistingAsync(id);
            var invoice = FindInvoice(existing, invoiceId);

            if (string.IsNullOrEmpty(invoice.PdfUrl))
            {
                throw new ArgumentException("This invoice has not been generated yet.");
            }

            return _blobStorageService.GetExpiringDownloadUri(invoice.PdfUrl, _documentLinkExpiryDays);
        }

        public async Task<FinancialCommunicationModel> SendCommunicationAsync(string id, string toEmail, string recipientType, string subject, string body,
            string? templateKey, string? relatedInvoiceId, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "send financial communications");
            var existing = await GetExistingAsync(id);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var htmlBody = body.Replace("\n", "<br/>");
            if (!string.IsNullOrEmpty(relatedInvoiceId))
            {
                var invoice = existing.Invoices.FirstOrDefault(i => i.Id == relatedInvoiceId);
                if (invoice != null && !string.IsNullOrEmpty(invoice.PdfUrl))
                {
                    var expiringUri = _blobStorageService.GetExpiringDownloadUri(invoice.PdfUrl, _documentLinkExpiryDays);
                    htmlBody += $"<p><strong>Invoice {invoice.InvoiceNo}:</strong> <a href=\"{expiringUri}\">Download PDF</a> (link expires in {_documentLinkExpiryDays} day{(_documentLinkExpiryDays == 1 ? "" : "s")}).</p>";
                }
            }

            await _emailService.SendEmailAsync(toEmail, subject, htmlBody, body);

            var communication = new FinancialCommunicationModel
            {
                TemplateKey = templateKey,
                ToEmail = toEmail,
                RecipientType = recipientType,
                Subject = subject,
                Body = body,
                RelatedInvoiceId = relatedInvoiceId,
                SentByUserId = actingUserId,
                SentByName = actingUserName,
                SentAt = DateTime.UtcNow
            };

            existing.Communications.Add(communication);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Sent email \"{subject}\" to {recipientType} ({toEmail})", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return communication;
        }

        private static string BuildInvoiceFileName(string invoiceNo, string partnerName, string studentName, DateTime issuedAt)
        {
            static string Sanitize(string value) =>
                string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim('_');

            var datePart = issuedAt.ToString("ddMMyyyy");
            return $"{Sanitize(invoiceNo)}-{Sanitize(partnerName)}-{Sanitize(studentName)}-{datePart}.pdf";
        }

        private static InvoiceModel FindInvoice(FinancialRecordModel record, string invoiceId)
        {
            return record.Invoices.FirstOrDefault(i => i.Id == invoiceId)
                ?? throw new KeyNotFoundException("Invoice not found");
        }

        private async Task<FinancialRecordModel> GetExistingAsync(string id)
        {
            return await _financialRecordDataAccess.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Financial record not found");
        }
    }
}