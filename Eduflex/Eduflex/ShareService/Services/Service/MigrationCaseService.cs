using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.DynamicForms;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Enums.VisaProcess;
using ShareService.Mapping;
using ShareService.Models.Enrolment;
using ShareService.Models.MigrationCase;
using ShareService.Models.Setting;
using ShareService.Models.VisaProcess;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    // The execution engine for the generic Migration Case system — see
    // docs/09-visa-process-config-module-design.md and
    // ShareService/Models/MigrationCase/MigrationCaseModel.cs for why this is a standalone
    // entity independent of EnrolmentModel/EnrolmentService. Deliberately mirrors
    // EnrolmentService's shape (ownership gate, ResolveUserNameAsync, AuditTrail entries)
    // wherever the same problem shows up, rather than inventing a different convention.
    public class MigrationCaseService : IMigrationCaseService
    {
        private readonly IMigrationCase _caseDataAccess;
        private readonly IVisaProcessTemplate _templateDataAccess;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly IAzureEmailService _emailService;
        private readonly IDynamicFormTemplate _dynamicFormTemplateDataAccess;
        private readonly IInvoicePdfService _pdfService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly ILogger<MigrationCaseService> _logger;
        private readonly int _documentLinkExpiryDays;

        public MigrationCaseService(
            IMigrationCase caseDataAccess,
            IVisaProcessTemplate templateDataAccess,
            IUserService userService,
            IRoleService roleService,
            IPermissionService permissionService,
            IAzureBlobDocStorageService blobStorageService,
            IAzureEmailService emailService,
            IDynamicFormTemplate dynamicFormTemplateDataAccess,
            IInvoicePdfService pdfService,
            IOptions<DocumentLinkSettings> documentLinkSettings,
            INotificationPublisher notificationPublisher,
            ILogger<MigrationCaseService> logger)
        {
            _caseDataAccess = caseDataAccess;
            _templateDataAccess = templateDataAccess;
            _userService = userService;
            _roleService = roleService;
            _permissionService = permissionService;
            _blobStorageService = blobStorageService;
            _emailService = emailService;
            _dynamicFormTemplateDataAccess = dynamicFormTemplateDataAccess;
            _pdfService = pdfService;
            _documentLinkExpiryDays = documentLinkSettings.Value.ExpiryDays;
            _notificationPublisher = notificationPublisher;
            _logger = logger;
        }

        // Auth: requires MigrationCasesView permission (staff-only).
        public async Task<PagedResult<MigrationCaseModel>> GetCasesAsync(MigrationCaseFilter filter, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.MigrationCasesView, "view migration cases");
            return await _caseDataAccess.GetCasesAsync(filter);
        }

        // Auth: requires MigrationCasesView permission (staff-only).
        public async Task<MigrationCaseModel?> GetCaseAsync(string id, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.MigrationCasesView, "view migration cases");
            return await _caseDataAccess.GetCaseAsync(id);
        }

        // Auth: requires MigrationCasesAdd permission. Snapshots the template's currently
        // Enabled steps (docs §C.3 — a disabled step never appears on a case at all) into
        // MigrationCaseStepModel instances; the first snapshotted step starts Draft, every
        // other Locked — same unlock semantics as VisaProcessStepModel.CreateDefault.
        public async Task<MigrationCaseModel> CreateCaseAsync(
            string templateId, string primaryContactName, string? primaryContactEmail,
            string? primaryContactMobile, string? notes, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.MigrationCasesAdd, "start migration cases");

            if (string.IsNullOrWhiteSpace(primaryContactName))
            {
                throw new ArgumentException("Primary contact name is required.");
            }

            var template = await _templateDataAccess.GetByIdAsync(templateId)
                ?? throw new KeyNotFoundException("Process template not found");

            if (template.Status != VisaTemplateStatus.Active.ToString())
            {
                throw new ArgumentException("This template is inactive and can't be used to start a new case.");
            }

            var enabledSteps = template.Steps.Where(s => s.Enabled).OrderBy(s => s.Order).ToList();
            if (enabledSteps.Count == 0)
            {
                throw new ArgumentException("This template has no enabled steps to start a case from.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var caseReference = await GenerateCaseReferenceAsync();

            var migrationCase = new MigrationCaseModel
            {
                OwnerUserId = actingUserId,
                CaseReference = caseReference,
                TemplateId = template.Id,
                TemplateVersion = template.Version,
                TemplateName = template.Name,
                Country = template.Country,
                Category = template.Category,
                PrimaryContactName = primaryContactName.Trim(),
                PrimaryContactEmail = primaryContactEmail,
                PrimaryContactMobile = primaryContactMobile,
                Notes = notes,
                Status = "Active",
                // Consultation always comes first (Order 0, Draft — see
                // MigrationCaseConsultationStep) and gates every templated step after it,
                // which now all start Locked regardless of position — mirrors Enrolment's
                // StudentInfo/EnrolmentForm always preceding the gated steps.
                Steps = new List<MigrationCaseStepModel> { MigrationCaseConsultationStep.Build() }
                    .Concat(enabledSteps.Select((s, i) => new MigrationCaseStepModel
                    {
                        Key = s.Key,
                        Order = i + 1,
                        Label = s.Label,
                        Description = s.Description,
                        Phase = s.Phase,
                        CanReopen = s.CanReopen,
                        PractitionerTagId = s.PractitionerTagId,
                        FieldsSnapshot = s.Fields,
                        RequiredEvidenceCategoriesSnapshot = s.RequiredEvidenceCategories,
                        PreconditionsSnapshot = s.Preconditions,
                        HintsSnapshot = s.Hints,
                        Status = "Locked"
                    }))
                    .ToList()
            };

            migrationCase.AuditTrail.Add(MigrationCaseAuditEntryModel.Create(
                $"Case created from template \"{template.Name}\" ({template.Country}/{template.Category})", actingUserId, actingUserName));

            await _caseDataAccess.CreateCaseAsync(migrationCase);

            await _notificationPublisher.PublishToRoleAsync(
                module: "MigrationCase",
                entityId: migrationCase.Id,
                summary: $"New {template.Category} case started for {migrationCase.PrimaryContactName}",
                role: SystemRole.Staff);

            _logger.LogInformation("Created migration case {CaseId} ({CaseReference}) from template {TemplateId}", migrationCase.Id, caseReference, templateId);
            return migrationCase;
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Editable any time, no completion/locking concept — unlike the Consultation step,
        // there's no reason to gate ordinary profile data behind a workflow step.
        public async Task<bool> UpdateCustomerInfoAsync(string id, MigrationCaseModel customerInfo, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            existing.ApplyCustomerInfo(customerInfo);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create("Updated customer info", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        public async Task<bool> SaveStepDraftAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var step = FindStep(existing, stepKey);

            if (step.Status == "Locked")
            {
                throw new ArgumentException("This step is locked — complete the previous step first.");
            }

            step.Fields = fields;
            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Enforces sequential gating, required evidence, and the step's PreconditionsSnapshot
        // (see EvaluatePreconditions) — the same three checks
        // EnrolmentService.CompleteVisaStepAsync runs, generalized to read from the snapshot
        // instead of the compiled VisaProcessStepKeys.
        public async Task<bool> CompleteStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var step = FindStep(existing, stepKey);

            if (step.Status == "Locked")
            {
                throw new ArgumentException("This step is locked — complete the previous step first.");
            }

            var missingCategory = step.RequiredEvidenceCategoriesSnapshot
                .FirstOrDefault(cat => !existing.Documents.Any(d => d.Category == cat));
            if (missingCategory != null)
            {
                throw new ArgumentException($"Upload the required \"{missingCategory}\" evidence document before marking this step complete.");
            }

            EvaluatePreconditions(existing, step, fields);

            step.Fields = fields;
            step.Status = "Complete";
            step.CompletedAt = DateTime.UtcNow;
            step.CompletedByUserId = actingUserId;
            step.CompletedByName = actingUserName;

            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Completed step \"{step.Label}\"", actingUserId, actingUserName));

            var orderedSteps = existing.Steps.OrderBy(s => s.Order).ToList();
            var currentIndex = orderedSteps.FindIndex(s => s.Key == stepKey);
            var isLastStep = currentIndex == orderedSteps.Count - 1;

            if (!isLastStep)
            {
                var nextStep = orderedSteps[currentIndex + 1];
                if (nextStep.Status == "Locked")
                {
                    nextStep.Status = "Draft";
                }
            }
            else
            {
                existing.Status = "Completed";
                existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create("Case marked Completed — final step done", actingUserId, actingUserName));
            }

            var saved = await _caseDataAccess.ReplaceCaseAsync(id, existing);

            if (saved)
            {
                await _notificationPublisher.PublishToRoleAsync(
                    module: "MigrationCase",
                    entityId: id,
                    summary: $"Case {existing.CaseReference} — step \"{step.Label}\" completed",
                    role: SystemRole.Staff);
            }

            return saved;
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Reopening a case's final step un-completes the case (flips Status back to
        // Active) — the generic-case equivalent of Enrolment's more elaborate
        // Finalize/ReopenVisaStep interaction, simplified since there's no separate
        // FinancialRecord-style terminal action here to guard against re-triggering.
        public async Task<bool> ReopenStepAsync(string id, string stepKey, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var step = FindStep(existing, stepKey);

            if (!step.CanReopen)
            {
                throw new ArgumentException("This step doesn't support reopening — edit its details directly instead.");
            }
            if (step.Status != "Complete")
            {
                throw new ArgumentException("Only a completed step can be reopened.");
            }

            step.Status = "Draft";
            step.CompletedAt = null;
            step.CompletedByUserId = null;
            step.CompletedByName = null;

            if (existing.Status == "Completed")
            {
                existing.Status = "Active";
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Reopened step \"{step.Label}\"", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Mirrors EnrolmentService.SendCommunicationAsync exactly — same expiring-SAS-link
        // attachment handling, same reasoning (nothing leaves blob storage unencrypted or
        // without an expiry). recipientType is a free string here rather than Enrolment's
        // fixed Student/EducationPartner/BusinessPartner set — a generic case has no
        // equivalent fixed recipient roles, the caller (frontend) defines whatever makes
        // sense for its own category.
        public async Task<EnrolmentCommunicationModel> SendCommunicationAsync(
            string id, string toEmail, string recipientType, string subject, string body,
            string? templateKey, List<string> attachedDocumentIds, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var htmlBody = body.Replace("\n", "<br/>");
            if (attachedDocumentIds.Count > 0)
            {
                var attachedDocuments = existing.Documents.Where(d => attachedDocumentIds.Contains(d.Id)).ToList();
                var links = attachedDocuments.Select(d =>
                {
                    var expiringUri = _blobStorageService.GetExpiringDownloadUri(d.Url, _documentLinkExpiryDays);
                    return $"<li><a href=\"{expiringUri}\">{d.FileName}</a></li>";
                });
                htmlBody += $"<p><strong>Attached documents</strong> (links expire in {_documentLinkExpiryDays} day{(_documentLinkExpiryDays == 1 ? "" : "s")}):</p><ul>{string.Join("", links)}</ul>";
            }

            await _emailService.SendEmailAsync(toEmail, subject, htmlBody, body);

            var communication = new EnrolmentCommunicationModel
            {
                TemplateKey = templateKey,
                ToEmail = toEmail,
                RecipientType = recipientType,
                Subject = subject,
                Body = body,
                AttachedDocumentIds = attachedDocumentIds,
                SentByUserId = actingUserId,
                SentByName = actingUserName,
                SentAt = DateTime.UtcNow
            };

            existing.Communications.Add(communication);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Sent email \"{subject}\" to {recipientType} ({toEmail})", actingUserId, actingUserName));

            await _caseDataAccess.ReplaceCaseAsync(id, existing);
            return communication;
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        public async Task<MigrationCaseDocumentModel> AddDocumentAsync(string id, MigrationCaseDocumentModel document, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            document.Id = Guid.NewGuid().ToString();
            document.UploadedByUserId = actingUserId;
            document.UploadedByName = actingUserName;
            document.UploadedAt = DateTime.UtcNow;

            existing.Documents.Add(document);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Uploaded document \"{document.FileName}\"", actingUserId, actingUserName));

            await _caseDataAccess.ReplaceCaseAsync(id, existing);
            return document;
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        public async Task<bool> DeleteDocumentAsync(string id, string documentId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var document = existing.Documents.FirstOrDefault(d => d.Id == documentId)
                ?? throw new KeyNotFoundException("Document not found");

            existing.Documents.Remove(document);

            try
            {
                await _blobStorageService.DeleteAsync(document.Url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MigrationCase {CaseId}: failed to delete blob for document {DocumentId}", id, documentId);
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Deleted document \"{document.FileName}\"", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: same two-ways-in shape as EnrolmentService.ReassignOwnerAsync — a manager
        // can reassign any case (MigrationCasesReassign), or the current owner can hand
        // their own case off to someone else (needs MigrationCasesEdit).
        public async Task<bool> ReassignOwnerAsync(string id, string newOwnerUserId, string actingUserId)
        {
            var existing = await _caseDataAccess.GetCaseAsync(id)
                ?? throw new KeyNotFoundException("Migration case not found");

            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            var canReassignAny = permissions.Contains(PermissionKey.MigrationCasesReassign.GetDescription());
            var isOwner = existing.OwnerUserId == actingUserId;

            if (!canReassignAny)
            {
                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Only the staff member who owns this case, or a manager, can reassign it.");
                }
                if (!permissions.Contains(PermissionKey.MigrationCasesEdit.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to edit migration cases.");
                }
            }

            var newOwner = await _userService.GetUserByIdAsync(newOwnerUserId)
                ?? throw new ArgumentException("The selected staff member was not found");

            var newOwnerRole = await _roleService.GetByIdAsync(newOwner.RoleId);
            if (newOwnerRole?.RoleType == RoleTypeEnums.Student)
            {
                throw new ArgumentException("Cannot reassign a migration case to a student.");
            }

            var previousOwnerName = await ResolveUserNameAsync(existing.OwnerUserId);
            var newOwnerName = $"{newOwner.FirstName} {newOwner.LastName}".Trim();
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            existing.OwnerUserId = newOwnerUserId;
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create(
                $"Reassigned owner from {previousOwnerName} to {newOwnerName}", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // ===================== Dynamic Forms =====================
        // Mirrors EnrolmentService's Dynamic Forms region (RequestForm/Withdraw/Archive/
        // Reopen/SetStatus/Export), reusing the same EnrolmentFormResponseModel and its
        // already-generic CreateFromTemplate/RenderToHtml/ValidateRequiredAnswers/
        // ValidateAnswerLengths extensions (ShareService.Mapping.
        // EnrolmentFormResponseMappingExtension). One deliberate difference: a Migration
        // Case contact has no portal account (unlike a student), so there is no
        // student-submits-via-emailed-link flow — SaveFormAnswersAsync below is the one
        // write path, used by staff to both draft and finalize a response on the
        // contact's behalf. No request/withdrawn/reopen emails either, for the same
        // reason — there's nowhere for the contact to click through to.

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        public async Task<EnrolmentFormResponseModel> RequestFormAsync(string id, string formTemplateId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var template = await _dynamicFormTemplateDataAccess.GetByIdAsync(formTemplateId)
                ?? throw new KeyNotFoundException("Form template not found");

            if (!template.Status.Is(TemplateStatus.Active))
            {
                throw new ArgumentException("This form is inactive and cannot be requested.");
            }

            if (existing.FormResponses.Any(r => r.FormTemplateId == formTemplateId && !r.Status.Is(ResponseStatus.Archived)))
            {
                throw new ArgumentException("This form has already been requested on this case. Withdraw and archive the existing request before requesting it again.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var response = template.CreateFromTemplate(actingUserId, actingUserName);

            existing.FormResponses.Add(response);
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Requested form \"{template.Name}\"", actingUserId, actingUserName));

            await _caseDataAccess.ReplaceCaseAsync(id, existing);
            return response;
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // The one write path for answers — see this region's header comment for why there's
        // no separate student-submit path. markSubmitted=false just saves a draft;
        // markSubmitted=true validates required questions, flips status to Responded, and
        // (re)generates the PDF snapshot — reachable from Requesting/Draft (first
        // finalization) or Responded (staff correcting an already-finalized answer).
        public async Task<bool> SaveFormAnswersAsync(string id, string responseId, List<FormAnswerModel> answers, bool markSubmitted, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (response.Status.Is(ResponseStatus.Withdrawn) || response.Status.Is(ResponseStatus.Archived))
            {
                throw new ArgumentException("This form request was withdrawn or archived and can no longer be edited.");
            }

            response.ValidateAnswerLengths(answers);
            if (markSubmitted)
            {
                response.ValidateRequiredAnswers(answers);
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var wasAlreadyResponded = response.Status.Is(ResponseStatus.Responded);
            response.Answers = answers;

            if (markSubmitted)
            {
                response.Status = ResponseStatus.Responded.ToString();
                response.SubmittedAt ??= DateTime.UtcNow;
                if (wasAlreadyResponded)
                {
                    response.StaffEditedAt = DateTime.UtcNow;
                    response.StaffEditedByUserId = actingUserId;
                }
                existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create(
                    wasAlreadyResponded ? $"Edited answers on submitted form \"{response.FormName}\"" : $"Submitted form \"{response.FormName}\"",
                    actingUserId, actingUserName));

                await SaveResponseAsDocumentAsync(existing, response, actingUserId, actingUserName);
            }
            else
            {
                response.Status = ResponseStatus.Draft.ToString();
                response.LastSavedAt = DateTime.UtcNow;
                existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Saved draft answers for form \"{response.FormName}\"", actingUserId, actingUserName));
            }

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        public async Task<bool> WithdrawFormRequestAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (response.Status.Is(ResponseStatus.Withdrawn) || response.Status.Is(ResponseStatus.Responded))
            {
                throw new ArgumentException("Only a requested or in-progress form can be withdrawn.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Withdrawn.ToString();
            response.WithdrawnAt = DateTime.UtcNow;
            response.WithdrawnByUserId = actingUserId;

            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Withdrew request for form \"{response.FormName}\"", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Only a withdrawn request can be archived — frees up its template for a fresh
        // request, same as EnrolmentService.ArchiveFormResponseAsync.
        public async Task<bool> ArchiveFormResponseAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (!response.Status.Is(ResponseStatus.Withdrawn))
            {
                throw new ArgumentException("Only a withdrawn request can be archived.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Archived.ToString();
            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Archived withdrawn form \"{response.FormName}\"", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Manual status override, same escape-hatch reasoning as
        // EnrolmentService.SetFormResponseStatusAsync.
        public async Task<bool> SetFormResponseStatusAsync(string id, string responseId, string newStatus, string actingUserId)
        {
            if (!Enum.TryParse<ResponseStatus>(newStatus, out _))
            {
                throw new ArgumentException("Not a recognised form status.");
            }

            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var previousStatus = response.Status;
            response.Status = newStatus;

            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Staff changed form \"{response.FormName}\" status from {previousStatus} to {newStatus}", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Reverses a finalized response back to Requesting so staff can gather updated
        // answers through SaveFormAnswersAsync again.
        public async Task<bool> ReopenFormForEditAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (!response.Status.Is(ResponseStatus.Responded))
            {
                throw new ArgumentException("Only a finalized (Responded) form can be reopened for edit.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Requesting.ToString();

            existing.AuditTrail.Add(MigrationCaseAuditEntryModel.Create($"Reopened form \"{response.FormName}\" for edit", actingUserId, actingUserName));

            return await _caseDataAccess.ReplaceCaseAsync(id, existing);
        }

        // Auth: requires MigrationCasesEdit permission + ownership — see GetOwnedCaseAsync.
        // Same on-demand render-to-bytes approach as EnrolmentService.ExportFormAsync, for
        // the same Content-Disposition reason.
        public async Task<(byte[] Content, string FileName)> ExportFormAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedCaseAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            var pdfBytes = await _pdfService.RenderToPdfAsync(response.RenderToHtml(existing.PrimaryContactName, existing.PrimaryContactEmail ?? "", existing.PrimaryContactMobile ?? ""));
            var fileName = $"{response.FormName}-{existing.PrimaryContactName}-{DateTime.UtcNow:dd MMM yyyy h.mmtt}.pdf";
            return (pdfBytes, fileName);
        }

        // Renders the finalized response to PDF and saves it as a Documents entry. Unlike
        // EnrolmentService's version, no StepEvidenceCategories lookup table is needed —
        // MigrationCaseDocumentModel.Category is already a free string (see
        // migration-case-detail.component.ts's documentCategories, built straight from each
        // step's own RequiredEvidenceCategoriesSnapshot), so a step-bound form's category is
        // just its BoundStepKey directly. Same replace-not-append behavior for a step-bound
        // form as Enrolment: each regeneration replaces the previous blob/Documents entry.
        private async Task SaveResponseAsDocumentAsync(MigrationCaseModel migrationCase, EnrolmentFormResponseModel response, string actingUserId, string actingUserName)
        {
            var pdfBytes = await _pdfService.RenderToPdfAsync(response.RenderToHtml(migrationCase.PrimaryContactName, migrationCase.PrimaryContactEmail ?? "", migrationCase.PrimaryContactMobile ?? ""));

            var isStepEvidence = !string.IsNullOrEmpty(response.BoundStepKey);
            var category = response.BoundStepKey ?? "DynamicForm";

            var fileName = $"{response.FormName}-{migrationCase.PrimaryContactName}-{DateTime.UtcNow:dd MMM yyyy h.mmtt}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            var url = await _blobStorageService.UploadAsync(stream, fileName, "application/pdf");

            var document = new MigrationCaseDocumentModel
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                Category = category,
                Url = url,
                ContentType = "application/pdf",
                SizeBytes = pdfBytes.LongLength,
                UploadedByUserId = actingUserId,
                UploadedByName = actingUserName,
                UploadedAt = DateTime.UtcNow
            };

            if (isStepEvidence && response.ExportedDocumentId != null)
            {
                var previous = migrationCase.Documents.FirstOrDefault(d => d.Id == response.ExportedDocumentId);
                if (previous != null)
                {
                    await _blobStorageService.DeleteAsync(previous.Url);
                    migrationCase.Documents.Remove(previous);
                }
            }

            migrationCase.Documents.Add(document);
            response.ExportedDocumentId = document.Id;
        }

        private static EnrolmentFormResponseModel FindFormResponse(MigrationCaseModel migrationCase, string responseId)
        {
            return migrationCase.FormResponses.FirstOrDefault(r => r.Id == responseId)
                ?? throw new KeyNotFoundException("Form response not found");
        }

        // Small closed evaluator over StepPreconditionType — deliberately not a rule
        // engine, see docs/09 §C.2. CourseApplicationFinalized is an Enrolment-only concept
        // (no course-application model exists on a generic case) — treated as
        // automatically satisfied here rather than thrown, so a template authored with
        // Enrolment's step set in mind doesn't hard-fail a generic case; it's on the
        // template author not to use it outside that context.
        private static void EvaluatePreconditions(MigrationCaseModel migrationCase, MigrationCaseStepModel step, Dictionary<string, string> submittedFields)
        {
            foreach (var precondition in step.PreconditionsSnapshot)
            {
                if (!Enum.TryParse<StepPreconditionType>(precondition.Type, out var type))
                {
                    continue;
                }

                switch (type)
                {
                    case StepPreconditionType.PriorStepFieldNotEmpty:
                        {
                            var sourceStep = migrationCase.Steps.FirstOrDefault(s => s.Key == precondition.SourceStepKey);
                            var hasValue = sourceStep != null
                                && !string.IsNullOrEmpty(precondition.FieldKey)
                                && sourceStep.Fields.TryGetValue(precondition.FieldKey, out var value)
                                && !string.IsNullOrEmpty(value);
                            if (!hasValue)
                            {
                                throw new ArgumentException(precondition.Detail ?? $"A required field on step \"{precondition.SourceStepKey}\" must be set first.");
                            }
                            break;
                        }
                    case StepPreconditionType.FieldValueIn:
                        {
                            var value = !string.IsNullOrEmpty(precondition.FieldKey) && submittedFields.TryGetValue(precondition.FieldKey, out var v) ? v : null;
                            if (value == null || !precondition.AllowedValues.Contains(value))
                            {
                                throw new ArgumentException(precondition.Detail ?? "This step's answer is not one of the allowed values.");
                            }
                            break;
                        }
                    case StepPreconditionType.AllPriorEvidenceUploaded:
                        {
                            var priorCategories = migrationCase.Steps
                                .Where(s => s.Order < step.Order)
                                .SelectMany(s => s.RequiredEvidenceCategoriesSnapshot)
                                .Distinct();
                            var missing = priorCategories.FirstOrDefault(cat => !migrationCase.Documents.Any(d => d.Category == cat));
                            if (missing != null)
                            {
                                throw new ArgumentException(precondition.Detail ?? $"Upload the required \"{missing}\" evidence document from an earlier step first.");
                            }
                            break;
                        }
                    case StepPreconditionType.CourseApplicationFinalized:
                        // Not applicable to a generic case — see method comment above.
                        break;
                }
            }
        }

        private static MigrationCaseStepModel FindStep(MigrationCaseModel migrationCase, string stepKey)
        {
            return migrationCase.Steps.FirstOrDefault(s => s.Key == stepKey)
                ?? throw new ArgumentException($"Unknown step \"{stepKey}\" on this case");
        }

        private async Task<MigrationCaseModel> GetOwnedCaseAsync(string id, string actingUserId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.MigrationCasesEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to edit migration cases");
            }

            var existing = await _caseDataAccess.GetCaseAsync(id)
                ?? throw new KeyNotFoundException("Migration case not found");

            if (existing.OwnerUserId != actingUserId)
            {
                throw new UnauthorizedAccessException("Only the staff member who owns this case can update it — ask a manager to reassign it to you first.");
            }

            return existing;
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

        private async Task<string> GenerateCaseReferenceAsync()
        {
            var count = await _caseDataAccess.CountAllAsync();
            return $"MIG-{DateTime.UtcNow:yyyy}-{count + 1:D5}";
        }
    }
}
