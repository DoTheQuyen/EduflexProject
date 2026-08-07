using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Course;
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
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly IBusinessPartner _businessPartnerDataAccess;
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly IAzureEmailService _emailService;
        private readonly IInvoicePdfService _invoicePdfService;
        private readonly INotificationPublisher _notificationPublisher;
        // Needed so the Communication tab's "attach invoice" picker can resolve invoices
        // from the new top-level Invoices collection — the old embedded Invoices list on
        // this record is no longer written to (see InvoiceService).
        private readonly IInvoice _invoiceDataAccess;
        private readonly ILogger<FinancialRecordService> _logger;
        private readonly int _documentLinkExpiryDays;

        public FinancialRecordService(
            IFinancialRecord financialRecordDataAccess,
            ICourse courseDataAccess,
            IEducationPartner educationPartnerDataAccess,
            IBusinessPartner businessPartnerDataAccess,
            IUserService userService,
            IPermissionService permissionService,
            IAzureBlobDocStorageService blobStorageService,
            IAzureEmailService emailService,
            IInvoicePdfService invoicePdfService,
            INotificationPublisher notificationPublisher,
            IInvoice invoiceDataAccess,
            IOptions<DocumentLinkSettings> documentLinkSettings,
            ILogger<FinancialRecordService> logger)
        {
            _financialRecordDataAccess = financialRecordDataAccess;
            _courseDataAccess = courseDataAccess;
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _businessPartnerDataAccess = businessPartnerDataAccess;
            _userService = userService;
            _permissionService = permissionService;
            _blobStorageService = blobStorageService;
            _emailService = emailService;
            _invoicePdfService = invoicePdfService;
            _notificationPublisher = notificationPublisher;
            _invoiceDataAccess = invoiceDataAccess;
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

            // A student can hold several course applications at once (see CourseApplicationModel) —
            // once one has been Finalized (required before CoE Completion can be marked
            // complete, which is what triggers this method), that's the course finance
            // should actually be billing against, not whatever single EducationPartnerId/
            // CourseId happen to still be sitting on the enrolment's own top-level fields.
            // Falls back to those enrolment-level fields for enrolments created before
            // this feature existed, which never got a CourseApplications entry at all.
            var finalizedCourseApplication = enrolment.CourseApplications.FirstOrDefault(c => c.Status == CourseApplicationStatuses.Finalized);
            var effectiveEducationPartnerId = finalizedCourseApplication?.EducationPartnerId ?? enrolment.EducationPartnerId;
            var effectiveCourseId = finalizedCourseApplication?.CourseId ?? enrolment.CourseId;

            var course = !string.IsNullOrEmpty(effectiveCourseId)
                ? await _courseDataAccess.GetCourseByIdAsync(effectiveCourseId)
                : null;
            var courseCommissionRate = course?.CommissionBaseRate ?? 0;
            var totalTuition = finalizedCourseApplication?.TuitionFee ?? enrolment.TuitionFee ?? course?.TuitionFee ?? 0;

            var effectiveBusinessPartnerId = enrolment.BusinessPartnerId;
            if (finalizedCourseApplication != null && !string.IsNullOrEmpty(effectiveEducationPartnerId))
            {
                var educationPartner = await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(effectiveEducationPartnerId);
                effectiveBusinessPartnerId = educationPartner?.BusinessPartnerId;
            }

            var businessPartner = !string.IsNullOrEmpty(effectiveBusinessPartnerId)
                ? await _businessPartnerDataAccess.GetBusinessPartnerByIdAsync(effectiveBusinessPartnerId)
                : null;
            var businessPartnerCommissionRate = businessPartner?.CommissionBaseRate ?? 0;

            var expectedCommission = businessPartner != null
                ? (courseCommissionRate / 100m) * (businessPartnerCommissionRate / 100m) * totalTuition
                : (courseCommissionRate / 100m) * totalTuition;

            var invoicePlan = BuildIntakePlan(course, enrolment.ActualCommencementDate);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var record = new FinancialRecordModel
            {
                EnrolmentId = enrolment.Id,
                CourseId = effectiveCourseId,
                ActualCommencementDate = enrolment.ActualCommencementDate,
                EducationPartnerId = effectiveEducationPartnerId,
                BusinessPartnerId = effectiveBusinessPartnerId,
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
                // Reads the new top-level Invoices collection, not this record's legacy
                // embedded Invoices list — nothing writes to that list anymore, so the old
                // lookup silently attached nothing.
                var invoice = await _invoiceDataAccess.GetByIdAsync(relatedInvoiceId);
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

        public async Task<FinancialRecordModel> RegenerateInvoicePlanAsync(string id, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "regenerate the invoice plan");
            var existing = await GetExistingAsync(id);

            var course = !string.IsNullOrEmpty(existing.CourseId)
                ? await _courseDataAccess.GetCourseByIdAsync(existing.CourseId)
                : null;
            var freshSlots = BuildIntakePlan(course, existing.ActualCommencementDate);

            // Match old auto-generated entries to fresh slots by IntakeDate so a claim
            // that's already Invoiced or Skipped (or had its date manually nudged) keeps
            // that state — only genuinely new/removed intake cycles change. Manual entries
            // (not derived from an intake) are never touched by this.
            var oldAuto = existing.InvoicePlan.Where(e => !e.IsManual).ToList();
            var manual = existing.InvoicePlan.Where(e => e.IsManual).ToList();

            foreach (var slot in freshSlots)
            {
                var match = oldAuto.FirstOrDefault(o => o.IntakeDate == slot.IntakeDate);
                if (match == null) continue;
                slot.Id = match.Id;
                slot.ClaimDate = match.ClaimDate;
                slot.Status = match.Status;
                slot.LinkedInvoiceId = match.LinkedInvoiceId;
                slot.SkipReason = match.SkipReason;
            }

            existing.InvoicePlan = freshSlots.Concat(manual).ToList();

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create(
                $"Regenerated the invoice request calendar ({freshSlots.Count} claim{(freshSlots.Count == 1 ? "" : "s")} from course intakes)",
                actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return existing;
        }

        public async Task<FinancialRecordModel> UpdatePlanEntryDateAsync(string id, string entryId, DateTime claimDate, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit the invoice plan");
            var existing = await GetExistingAsync(id);
            var entry = FindPlanEntry(existing, entryId);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create(
                $"Changed claim date from {entry.ClaimDate:dd/MM/yyyy} to {claimDate:dd/MM/yyyy}", actingUserId, actingUserName));
            entry.ClaimDate = claimDate;

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return existing;
        }

        public async Task<FinancialRecordModel> SkipPlanEntryAsync(string id, string entryId, string? reason, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit the invoice plan");
            var existing = await GetExistingAsync(id);
            var entry = FindPlanEntry(existing, entryId);
            if (entry.Status == "Invoiced")
            {
                throw new ArgumentException("This claim already has an invoice — cancel the invoice instead of skipping the claim.");
            }

            entry.Status = "Skipped";
            entry.SkipReason = reason;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var suffix = string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}";
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Skipped claim due {entry.ClaimDate:dd/MM/yyyy}{suffix}", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return existing;
        }

        public async Task<FinancialRecordModel> RestorePlanEntryAsync(string id, string entryId, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit the invoice plan");
            var existing = await GetExistingAsync(id);
            var entry = FindPlanEntry(existing, entryId);
            if (entry.Status != "Skipped")
            {
                throw new ArgumentException("This claim isn't skipped.");
            }

            entry.Status = "Planned";
            entry.SkipReason = null;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Restored claim due {entry.ClaimDate:dd/MM/yyyy}", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return existing;
        }

        public async Task<FinancialRecordModel> AddManualPlanEntryAsync(string id, DateTime claimDate, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.FinanceEdit, "edit the invoice plan");
            var existing = await GetExistingAsync(id);

            existing.InvoicePlan.Add(new InvoicePlanEntryModel
            {
                ClaimDate = claimDate,
                Status = "Planned",
                IsManual = true
            });

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(FinancialAuditEntryModel.Create($"Added a manual claim for {claimDate:dd/MM/yyyy}", actingUserId, actingUserName));

            await _financialRecordDataAccess.ReplaceAsync(id, existing);
            return existing;
        }

        private static InvoicePlanEntryModel FindPlanEntry(FinancialRecordModel record, string entryId) =>
            record.InvoicePlan.FirstOrDefault(e => e.Id == entryId) ?? throw new KeyNotFoundException("Invoice plan entry not found");

        // Claims recur once per intake cycle (every 12/intakes.Count months) for as many
        // cycles as fit in the course duration, each due 45 days after its intake. The
        // first claim is anchored to the intake at-or-before the student's own
        // commencement month, so a student starting in the Feb intake of a Feb/Aug course
        // gets claims at Feb, Aug, Feb, Aug... — see the approved mockup for the worked
        // example this mirrors exactly (24-month course, 2 intakes/year -> 4 claims).
        private static List<InvoicePlanEntryModel> BuildIntakePlan(CourseModel? course, DateTime? commencementDate)
        {
            if (course == null || !commencementDate.HasValue || course.Intakes.Count == 0
                || !course.CourseDurationMonths.HasValue || course.CourseDurationMonths.Value <= 0)
            {
                return new List<InvoicePlanEntryModel>();
            }

            var intakeMonths = course.Intakes
                .Select(ParseIntakeMonth)
                .Where(m => m.HasValue)
                .Select(m => m!.Value)
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            if (intakeMonths.Count == 0) return new List<InvoicePlanEntryModel>();

            var claimCount = Math.Max(1, (int)Math.Round(
                course.CourseDurationMonths.Value * intakeMonths.Count / 12.0, MidpointRounding.AwayFromZero));

            var startMonth = commencementDate.Value.Month;
            var startIndex = 0;
            for (var i = 0; i < intakeMonths.Count; i++)
            {
                if (intakeMonths[i] <= startMonth) { startIndex = i; }
            }

            var entries = new List<InvoicePlanEntryModel>();
            var idx = startIndex;
            var year = commencementDate.Value.Year;
            for (var i = 0; i < claimCount; i++)
            {
                var intakeDate = new DateTime(year, intakeMonths[idx], 1);
                entries.Add(new InvoicePlanEntryModel
                {
                    IntakeDate = intakeDate,
                    ClaimDate = intakeDate.AddDays(45),
                    Status = "Planned"
                });

                idx++;
                if (idx >= intakeMonths.Count) { idx = 0; year++; }
            }

            return entries;
        }

        // Course.Intakes stores free-text month labels (e.g. "February") rather than
        // exact dates — tolerates full and short month names, case/whitespace-insensitive.
        // Labels that don't parse to a month (a typo, or something like "Semester 1") are
        // silently skipped rather than failing the whole plan.
        private static int? ParseIntakeMonth(string label)
        {
            var trimmed = label.Trim();
            if (DateTime.TryParseExact(trimmed, new[] { "MMMM", "MMM" }, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            {
                return parsed.Month;
            }
            return null;
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