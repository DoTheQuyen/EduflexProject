using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.DynamicForms;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Mapping;
using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.Enquiry;
using ShareService.Models.Enrolment;
using ShareService.Models.Setting;
using ShareService.Models.Student;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;

namespace ShareService.Services
{
    public class EnrolmentService : IEnrolmentService
    {
        private readonly IEnrolment _enrolmentDataAccess;
        private readonly IApplication _applicationDataAccess;
        private readonly IEnquiryService _enquiryService;
        private readonly IUserService _userService;
        private readonly IStudentService _studentService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureEmailService _emailService;
        private readonly IAzureBlobDocStorageService _blobStorageService;
        private readonly IEducationPartner _educationPartnerDataAccess;
        private readonly ICourse _courseDataAccess;
        private readonly ISettings _settingsDataAccess;
        private readonly IFinancialRecordService _financialRecordService;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IDynamicFormTemplate _dynamicFormTemplateDataAccess;
        private readonly IEmailTemplate _emailTemplateDataAccess;
        private readonly IInvoicePdfService _pdfService;
        private readonly IValidator<EnrolmentModel> _validator;
        private readonly ILogger<EnrolmentService> _logger;
        private readonly int _documentLinkExpiryDays;
        private readonly string _frontendBaseUrl;

        public EnrolmentService(
            IEnrolment enrolmentDataAccess,
            IApplication applicationDataAccess,
            IEnquiryService enquiryService,
            IUserService userService,
            IStudentService studentService,
            IRoleService roleService,
            IPermissionService permissionService,
            IAzureEmailService emailService,
            IAzureBlobDocStorageService blobStorageService,
            IEducationPartner educationPartnerDataAccess,
            ICourse courseDataAccess,
            ISettings settingsDataAccess,
            IFinancialRecordService financialRecordService,
            INotificationPublisher notificationPublisher,
            IDynamicFormTemplate dynamicFormTemplateDataAccess,
            IEmailTemplate emailTemplateDataAccess,
            IInvoicePdfService pdfService,
            IValidator<EnrolmentModel> validator,
            IOptions<DocumentLinkSettings> documentLinkSettings,
            IOptions<WebURLSettings> webUrlSettings,
            ILogger<EnrolmentService> logger)
        {
            _enrolmentDataAccess = enrolmentDataAccess;
            _applicationDataAccess = applicationDataAccess;
            _enquiryService = enquiryService;
            _userService = userService;
            _studentService = studentService;
            _roleService = roleService;
            _permissionService = permissionService;
            _emailService = emailService;
            _blobStorageService = blobStorageService;
            _educationPartnerDataAccess = educationPartnerDataAccess;
            _courseDataAccess = courseDataAccess;
            _settingsDataAccess = settingsDataAccess;
            _financialRecordService = financialRecordService;
            _notificationPublisher = notificationPublisher;
            _dynamicFormTemplateDataAccess = dynamicFormTemplateDataAccess;
            _emailTemplateDataAccess = emailTemplateDataAccess;
            _pdfService = pdfService;
            _validator = validator;
            _documentLinkExpiryDays = documentLinkSettings.Value.ExpiryDays;
            _frontendBaseUrl = webUrlSettings.Value.FrontendBaseUrl;
            _logger = logger;
        }

        // Re-derives the server-owned BusinessPartnerId link and, only when no tuition
        // fee has been set yet, prefills TuitionFee from the linked Course. Called from
        // both create and update — "load from course, editable" is a one-time prefill,
        // never a silent overwrite of a value staff already edited.
        private async Task ApplyDerivedLinkageAsync(EnrolmentModel enrolment)
        {
            if (!string.IsNullOrEmpty(enrolment.EducationPartnerId))
            {
                var partner = await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(enrolment.EducationPartnerId);
                enrolment.BusinessPartnerId = partner?.BusinessPartnerId;
            }
            else
            {
                enrolment.BusinessPartnerId = null;
            }

            if ((enrolment.TuitionFee is null or 0) && !string.IsNullOrEmpty(enrolment.CourseId))
            {
                var course = await _courseDataAccess.GetCourseByIdAsync(enrolment.CourseId);
                if (course != null)
                {
                    enrolment.TuitionFee = course.TuitionFee;
                }
            }
        }

        // Auth: requires EnquiryView permission too (via GetEnquiryAsync below) — see
        // CreateInternalAsync for the EnrolmentsAdd check itself.
        public async Task<EnrolmentModel> CreateFromEnquiryAsync(string enquiryId, EnrolmentModel input, string? existingStudentId, string actingUserId)
        {
            var enquiry = await _enquiryService.GetEnquiryAsync(enquiryId, actingUserId)
                ?? throw new KeyNotFoundException("Enquiry not found");

            var alreadyConverted = await _enrolmentDataAccess.GetEnrolmentByEnquiryIdAsync(enquiryId);
            if (alreadyConverted != null)
            {
                throw new ArgumentException("This enquiry has already been converted to an enrolment.");
            }

            return await CreateInternalAsync(input, existingStudentId, actingUserId, enquiryId, enquiry);
        }

        // Auth: requires EnrolmentsAdd permission — see CreateInternalAsync below.
        public async Task<EnrolmentModel> CreateIndependentAsync(EnrolmentModel input, string? existingStudentId, string actingUserId)
        {
            return await CreateInternalAsync(input, existingStudentId, actingUserId, null, null);
        }

        // Auth: requires EnrolmentsAdd permission, PLUS — because this method also
        // creates (or links to) the student's login account and (if from an enquiry)
        // updates that enquiry — the caller effectively needs UsersAdd too (checked
        // inside UserService.CreateUserAsync below, only on the new-student path),
        // StudentsView (checked inside StudentService.GetStudentAsync, only on the
        // existing-student path) and, for the from-enquiry path, EnquiryView/EnquiryEdit
        // (checked in EnquiryService). A composite action needs every permission its
        // sub-actions need; there's no single key that covers it.
        private async Task<EnrolmentModel> CreateInternalAsync(EnrolmentModel input, string? existingStudentId, string actingUserId, string? enquiryId, EnquiryModel? sourceEnquiry)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.EnrolmentsAdd.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to create enrolments");
            }

            var validation = await _validator.ValidateAsync(input, options => options.IncludeRuleSets("default", "Create"));
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var (studentUserId, studentApplicationId, linkAuditDescription) = !string.IsNullOrWhiteSpace(existingStudentId)
                ? await LinkToExistingStudentAsync(existingStudentId!, input, actingUserId)
                : await CreateNewStudentAsync(input, actingUserId);

            // Create the staff-facing enrolment record that links everything together.
            input.Id = string.Empty;
            input.OwnerUserId = actingUserId;
            input.StudentUserId = studentUserId;
            input.StudentApplicationId = studentApplicationId;
            input.EnquiryId = enquiryId;
            if (string.IsNullOrWhiteSpace(input.Status))
            {
                input.Status = EnrolmentEnums.Draft.ToString();
            }

            await ApplyDerivedLinkageAsync(input);

            // Student Info + Enrolment Form are captured right here as part of creating the
            // enrolment, so the VISA Process workflow starts with both already Complete and
            // Apply Offer unlocked — see VisaProcessStepModel.CreateDefault.
            input.VisaProcessSteps = VisaProcessStepModel.CreateDefault(actingUserId, actingUserName);

            input.AuditTrail = new List<EnrolmentAuditEntryModel>
            {
                sourceEnquiry != null
                    ? EnrolmentAuditEntryModel.Create($"Converted from Enquiry {sourceEnquiry.FirstName} {sourceEnquiry.LastName} (#{sourceEnquiry.Id})", actingUserId, actingUserName)
                    : EnrolmentAuditEntryModel.Create("Enrolment created directly by staff", actingUserId, actingUserName),
                EnrolmentAuditEntryModel.Create(linkAuditDescription, actingUserId, actingUserName),
                EnrolmentAuditEntryModel.Create("VISA Process started — Student Info and Enrolment Form steps completed, Apply Offer unlocked", actingUserId, actingUserName)
            };

            await _enrolmentDataAccess.CreateEnrolmentAsync(input);

            await _notificationPublisher.PublishToRoleAsync(
                module: "Enrolment",
                entityId: input.Id,
                summary: $"New enrolment created for {input.FirstName} {input.LastName}",
                role: SystemRole.Staff);

            // If converted from an enquiry, flip its status so the link shows up on both sides.
            if (sourceEnquiry != null)
            {
                await MarkSourceEnquiryConvertedAsync(sourceEnquiry, input.Id, actingUserId);
            }

            _logger.LogInformation("Created enrolment {EnrolmentId} for {Email} (enquiry: {EnquiryId})", input.Id, input.Email, enquiryId ?? "none");

            return input;
        }

        // Step 1 of the New Enrolment wizard already resolved (found via search, or just
        // created) a Students collection record — link to that existing login rather than
        // creating a second one for the same person.
        private async Task<(string StudentUserId, string StudentApplicationId, string LinkAuditDescription)> LinkToExistingStudentAsync(
            string existingStudentId, EnrolmentModel input, string actingUserId)
        {
            var existingStudent = await _studentService.GetStudentAsync(existingStudentId, actingUserId)
                ?? throw new KeyNotFoundException("Selected student not found");

            var linkedApplication = input.ToEnrolmentApplicationModel(existingStudentId);
            await _applicationDataAccess.CreateApplicationAsync(linkedApplication);

            return (existingStudent.Student.UserId, linkedApplication.Id, $"Linked to existing student account for {input.Email}");
        }

        private async Task<(string StudentUserId, string StudentApplicationId, string LinkAuditDescription)> CreateNewStudentAsync(
            EnrolmentModel input, string actingUserId)
        {
            var roles = await _roleService.GetAllRolesAsync();
            var studentRole = roles.FirstOrDefault(r => r.Name == SystemRole.Student.ToString())
                ?? throw new InvalidOperationException("The Student role is not configured — seed the Roles collection first.");

            // 1. Create the student's login account. UserService already hashes the password,
            //    forces a change on first login and emails the temp credentials — see UserService.CreateUserAsync.
            var newUser = input.ToNewUserModel(studentRole.Id, GenerateTempPassword());
            await _userService.CreateUserAsync(newUser, actingUserId);

            // 2. Create the Student profile + a starter Application record so the student can log in
            //    and immediately see something to attach documents to (existing Applications/Students infra).
            var student = input.ToStudentModel(newUser.Id);
            await _applicationDataAccess.CreateStudentAsync(student);

            var newApplication = input.ToEnrolmentApplicationModel(student.Id);
            await _applicationDataAccess.CreateApplicationAsync(newApplication);

            return (newUser.Id, newApplication.Id, $"Created student user account for {input.Email}");
        }

        private async Task MarkSourceEnquiryConvertedAsync(EnquiryModel sourceEnquiry, string enrolmentId, string actingUserId)
        {
            sourceEnquiry.Status = EnquiryEnums.Converted.ToString();
            if (string.IsNullOrWhiteSpace(sourceEnquiry.Response))
            {
                sourceEnquiry.Response = $"Converted to enrolment #{enrolmentId}.";
            }
            await _enquiryService.UpdateEnquiriesAsync(sourceEnquiry.Id, sourceEnquiry, actingUserId);
        }

        // Auth: requires EnrolmentsView permission (staff-only).
        public async Task<PagedResult<EnrolmentModel>> GetEnrolmentsAsync(EnrolmentFilter filter, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.EnrolmentsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view enrolments");
            }

            return await _enrolmentDataAccess.GetEnrolmentsAsync(filter);
        }

        // Auth: requires EnrolmentsView permission (staff-only).
        public async Task<EnrolmentModel?> GetEnrolmentAsync(string id, string userId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(PermissionKey.EnrolmentsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view enrolments");
            }

            var enrolment = await _enrolmentDataAccess.GetEnrolmentAsync(id);

            // Backfill for enrolments created before the VISA Process workflow existed —
            // seeded on read rather than migrated, since Mongo is schemaless and there's
            // no per-step history to reconstruct for these older records anyway.
            if (enrolment != null && enrolment.VisaProcessSteps.Count == 0)
            {
                var ownerName = await ResolveUserNameAsync(enrolment.OwnerUserId);
                enrolment.VisaProcessSteps = VisaProcessStepModel.CreateDefault(enrolment.OwnerUserId, ownerName);
            }

            return enrolment;
        }

        // Auth: requires EnrolmentsView permission (staff-only) — same permission as the
        // general enrolments list, pre-filtered to one student for the Student Details
        // page's history panel. Takes the student's UserId (EnrolmentModel.StudentUserId),
        // not the Student record's Mongo Id — those are different keys in this app.
        public async Task<List<EnrolmentModel>> GetEnrolmentsForStudentAsync(string studentUserId, string actingUserId)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.EnrolmentsView.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to view enrolments");
            }

            return await _enrolmentDataAccess.GetByStudentUserIdAsync(studentUserId);
        }

        // Auth: requires EnrolmentsEdit permission AND ownership (only the staff member
        // who owns this enrolment may update it) — both enforced inside
        // GetOwnedEnrolmentAsync below, which every mutating method in this class goes
        // through.
        public async Task<bool> UpdateEnrolmentAsync(string id, EnrolmentModel updateModel, string actingUserId)
        {
            var validation = await _validator.ValidateAsync(updateModel, options => options.IncludeRuleSets("default", "Update"));
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            existing.ApplyEditableFields(updateModel);
            await ApplyDerivedLinkageAsync(existing);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create("Updated enrolment details", actingUserId, actingUserName));

            var saved = await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            if (saved)
            {
                await _notificationPublisher.PublishToRoleAsync(
                    module: "Enrolment",
                    entityId: id,
                    summary: $"Enrolment updated for {existing.FirstName} {existing.LastName}",
                    role: SystemRole.Staff);
            }

            // A student may only start a new application while their current one is
            // Pending/Approved — once staff actually enrol them (this transition),
            // the source application is done being "active" and should reflect that.
            if (saved && existing.Status == EnrolmentEnums.VisaSuccess.ToString() && !string.IsNullOrEmpty(existing.StudentApplicationId))
            {
                await _applicationDataAccess.UpdateApplicationStatusAsync(existing.StudentApplicationId, "Studying");
            }

            return saved;
        }

        // Auth: requires EnrolmentsReassign permission (Manager/Admin only — Staff gets
        // View/Add/Edit/Delete but not Reassign, per the seeding migration).
        public async Task<bool> ReassignOwnerAsync(string id, string newOwnerUserId, string actingUserId)
        {
            var existing = await _enrolmentDataAccess.GetEnrolmentAsync(id)
                ?? throw new KeyNotFoundException("Enrolment not found");

            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            var canReassignAny = permissions.Contains(PermissionKey.EnrolmentsReassign.GetDescription());
            var isOwner = existing.OwnerUserId == actingUserId;

            // Two ways in: a manager can reassign any enrolment (EnrolmentsReassign), or
            // the current owner can hand their own enrolment off to someone else — the
            // same "self-service, needs Edit" bar as every other mutating action on this
            // enrolment (see GetOwnedEnrolmentAsync). Neither alone is enough for someone
            // who is neither the owner nor a manager.
            if (!canReassignAny)
            {
                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Only the staff member who owns this enrolment, or a manager, can reassign it.");
                }
                if (!permissions.Contains(PermissionKey.EnrolmentsEdit.GetDescription()))
                {
                    throw new UnauthorizedAccessException("You do not have permission to edit enrolments.");
                }
            }

            var newOwner = await _userService.GetUserByIdAsync(newOwnerUserId)
                ?? throw new ArgumentException("The selected staff member was not found");

            // Server-side enforcement, independent of what the UI happens to offer — a
            // direct API call reassigning to a student must be rejected the same as a
            // UI-driven one (the frontend already filters students out of the picker, but
            // that alone isn't enforcement).
            var newOwnerRole = await _roleService.GetByIdAsync(newOwner.RoleId);
            if (newOwnerRole?.RoleType == RoleTypeEnums.Student)
            {
                throw new ArgumentException("Cannot reassign an enrolment to a student.");
            }

            var previousOwnerName = await ResolveUserNameAsync(existing.OwnerUserId);
            var newOwnerName = $"{newOwner.FirstName} {newOwner.LastName}".Trim();
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            existing.OwnerUserId = newOwnerUserId;
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create(
                $"Reassigned owner from {previousOwnerName} to {newOwnerName}", actingUserId, actingUserName));

            var reassigned = await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            if (reassigned)
            {
                var summary = $"Enrolment reassigned from {previousOwnerName} to {newOwnerName}";
                await _notificationPublisher.PublishToRoleAsync("Enrolment", id, summary, SystemRole.Manager);
                await _notificationPublisher.PublishToRoleAsync("Enrolment", id, summary, SystemRole.Admin);
            }

            return reassigned;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<EnrolmentDocumentModel> AddDocumentAsync(string id, EnrolmentDocumentModel document, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            document.Id = Guid.NewGuid().ToString();
            document.UploadedByUserId = actingUserId;
            document.UploadedByName = actingUserName;
            document.IsFromStudent = false;
            document.UploadedAt = DateTime.UtcNow;

            existing.Documents.Add(document);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Uploaded document \"{document.FileName}\"", actingUserId, actingUserName));

            await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
            return document;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Also carries the document's Note field, so a single "edit this file" action in
        // the uploader zones can update the filename and note together.
        public async Task<bool> RenameDocumentAsync(string id, string documentId, string newFileName, string? note, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var document = existing.Documents.FirstOrDefault(d => d.Id == documentId)
                ?? throw new KeyNotFoundException("Document not found");

            var oldName = document.FileName;
            document.FileName = newFileName;
            document.Note = note;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Renamed document \"{oldName}\" to \"{newFileName}\"", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<bool> DeleteDocumentAsync(string id, string documentId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var document = existing.Documents.FirstOrDefault(d => d.Id == documentId)
                ?? throw new KeyNotFoundException("Document not found");

            existing.Documents.Remove(document);

            try
            {
                await _blobStorageService.DeleteAsync(document.Url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enrolment {EnrolmentId}: failed to delete blob for document {DocumentId}", id, documentId);
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Deleted document \"{document.FileName}\"", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // toEmail/recipientType let staff email the student, the linked education partner,
        // or a manually-entered business-partner address instead of always the student.
        // attachedDocumentIds are resolved to time-limited SAS links (not real email
        // attachments) and appended to the body, so nothing leaves blob storage unencrypted
        // or without an expiry.
        public async Task<EnrolmentCommunicationModel> SendCommunicationAsync(string id, string toEmail, string recipientType, string subject, string body, string? templateKey, List<string> attachedDocumentIds, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
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
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Sent email \"{subject}\" to {recipientType} ({toEmail})", actingUserId, actingUserName));

            await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
            return communication;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<bool> SaveVisaStepDraftAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var step = FindStep(existing, stepKey);

            if (step.Status == "Locked")
            {
                throw new ArgumentException("This step is locked — complete the previous step first.");
            }

            step.Fields = fields;
            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Unlike SaveVisaStepDraftAsync (which replaces the whole Fields bag with whatever
        // the frontend form currently holds), this merges just the given keys — used by
        // InvoiceService so recording invoiceId/invoiceSentAt/invoicePaidAt can never wipe
        // out an unrelated field (e.g. a staff comment) saved on the same step.
        public async Task SetStepFieldsAsync(string id, string stepKey, Dictionary<string, string> fieldsToMerge, string auditDescription, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var step = FindStep(existing, stepKey);

            foreach (var (key, value) in fieldsToMerge)
            {
                step.Fields[key] = value;
            }

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create(auditDescription, actingUserId, actingUserName));
            await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // ----- Course Applications (Enrolment Form step's nested sub-panels) -----

        private async Task<int> GetEffectiveMaxApplicationsAsync(EnrolmentModel enrolment)
        {
            var settings = await _settingsDataAccess.GetSettingsAsync();
            var globalMax = settings?.MaxApplicationsPerStudent ?? 1;
            // The override can never exceed the current global ceiling — if an admin
            // lowers the global setting after an override was saved, the lower ceiling
            // wins from that point on rather than honouring the stale override.
            return enrolment.MaxApplicationsOverride.HasValue
                ? Math.Min(enrolment.MaxApplicationsOverride.Value, globalMax)
                : globalMax;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<bool> SetMaxApplicationsOverrideAsync(string id, int newMax, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var settings = await _settingsDataAccess.GetSettingsAsync();
            var globalMax = settings?.MaxApplicationsPerStudent ?? 1;

            if (newMax < 1)
            {
                throw new ArgumentException("Number of applications allowed must be at least 1.");
            }
            if (newMax > globalMax)
            {
                throw new ArgumentException($"Number of applications allowed can't exceed the system maximum ({globalMax}).");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.MaxApplicationsOverride = newMax;
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Set applications allowed to {newMax}", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<CourseApplicationModel> AddCourseApplicationAsync(string id, string educationPartnerId, string courseId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);

            var activeCount = existing.CourseApplications.Count(c => c.Status != CourseApplicationStatuses.Withdrawn);
            var effectiveMax = await GetEffectiveMaxApplicationsAsync(existing);
            if (activeCount >= effectiveMax)
            {
                throw new ArgumentException($"This student is already at their application limit ({effectiveMax}). Increase \"Number applications allowed\" first.");
            }

            var course = await _courseDataAccess.GetCourseByIdAsync(courseId);

            var courseApplication = new CourseApplicationModel
            {
                EducationPartnerId = educationPartnerId,
                CourseId = courseId,
                Status = CourseApplicationStatuses.Init,
                TuitionFee = course?.TuitionFee
            };
            existing.CourseApplications.Add(courseApplication);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create("Added a course application", actingUserId, actingUserName));

            await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
            return courseApplication;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Editable any time except once Withdrawn — a Finalized entry can still be
        // corrected (e.g. a typo in intake), same "the winning one might need a fix"
        // reasoning as VisaOutcome staying editable after completion.
        public async Task<bool> UpdateCourseApplicationDetailsAsync(string id, string courseApplicationId, CourseApplicationDetailsModel details, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var courseApplication = existing.CourseApplications.FirstOrDefault(c => c.Id == courseApplicationId)
                ?? throw new KeyNotFoundException("Course application not found");

            if (courseApplication.Status == CourseApplicationStatuses.Withdrawn)
            {
                throw new ArgumentException("A withdrawn course application can't be edited.");
            }

            courseApplication.Intake = details.Intake;
            courseApplication.StudyMode = details.StudyMode;
            courseApplication.Campus = details.Campus;
            courseApplication.CommencementDate = details.CommencementDate;
            courseApplication.ExpectedCompletionDate = details.ExpectedCompletionDate;
            courseApplication.ActualCommencementDate = details.ActualCommencementDate;
            courseApplication.Notes = details.Notes;
            if (details.TuitionFee.HasValue)
            {
                courseApplication.TuitionFee = details.TuitionFee;
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create("Updated course application details", actingUserId, actingUserName));
            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // General status changes (Init/Applied/Offered/Withdrawn) — Finalized is
        // deliberately excluded here, see FinalizeCourseApplicationAsync for why it needs
        // its own dedicated action instead of being just another value on this setter.
        public async Task<bool> SetCourseApplicationStatusAsync(string id, string courseApplicationId, string newStatus, string actingUserId)
        {
            if (newStatus == CourseApplicationStatuses.Finalized)
            {
                throw new ArgumentException("Use the Finalize action to move a course application to Finalized.");
            }
            var validStatuses = new[] { CourseApplicationStatuses.Init, CourseApplicationStatuses.Applied, CourseApplicationStatuses.Offered, CourseApplicationStatuses.Withdrawn };
            if (!validStatuses.Contains(newStatus))
            {
                throw new ArgumentException($"\"{newStatus}\" is not a valid status.");
            }

            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var courseApplication = existing.CourseApplications.FirstOrDefault(c => c.Id == courseApplicationId)
                ?? throw new KeyNotFoundException("Course application not found");

            if (courseApplication.Status == CourseApplicationStatuses.Finalized)
            {
                throw new ArgumentException("This course application has already been finalized and can't be changed here.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            courseApplication.Status = newStatus;
            courseApplication.StatusUpdatedAt = DateTime.UtcNow;
            courseApplication.StatusUpdatedByName = actingUserName;

            // Captured once, the first time this application reaches Offered — later
            // transitions (including a possible bounce back through other statuses) never
            // overwrite it, so it stays the true "offer applied" date for the Finalize
            // confirmation to show.
            if (newStatus == CourseApplicationStatuses.Offered && courseApplication.OfferAppliedDate == null)
            {
                courseApplication.OfferAppliedDate = courseApplication.StatusUpdatedAt;
            }

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Course application status changed to \"{newStatus}\"", actingUserId, actingUserName));
            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Only reachable from Offered (enforced here, not just the frontend button's
        // disabled state) — finalizing represents "this is the one course the student is
        // actually going ahead with", so every sibling course application on this
        // enrolment is withdrawn in the same call. This is also the precondition
        // CompleteVisaStepAsync checks before allowing CoE Completion to be marked done.
        public async Task<bool> FinalizeCourseApplicationAsync(string id, string courseApplicationId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var courseApplication = existing.CourseApplications.FirstOrDefault(c => c.Id == courseApplicationId)
                ?? throw new KeyNotFoundException("Course application not found");

            if (courseApplication.Status != CourseApplicationStatuses.Offered)
            {
                throw new ArgumentException("Only a course application with status \"Offered\" can be finalized.");
            }

            // The whole point of Finalize is "this is the offer the student is accepting"
            // — can't make that call without the offer letter actually attached to this
            // specific application (see the per-application "UniOffer" upload zone on the
            // course application panel).
            if (!existing.Documents.Any(d => d.CourseApplicationId == courseApplicationId && d.Category == "UniOffer"))
            {
                throw new ArgumentException("Attach the university offer letter to this course application before finalizing.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var now = DateTime.UtcNow;

            courseApplication.Status = CourseApplicationStatuses.Finalized;
            courseApplication.StatusUpdatedAt = now;
            courseApplication.StatusUpdatedByName = actingUserName;

            var siblingIds = existing.CourseApplications
                .Where(c => c.Id != courseApplicationId && c.Status != CourseApplicationStatuses.Withdrawn)
                .Select(c => c.Id)
                .ToList();

            foreach (var sibling in existing.CourseApplications.Where(c => siblingIds.Contains(c.Id)))
            {
                sibling.Status = CourseApplicationStatuses.Withdrawn;
                sibling.StatusUpdatedAt = now;
                sibling.StatusUpdatedByName = actingUserName;
            }

            // Once one offer is accepted, the other universities' offer letters no longer
            // serve any purpose and are removed outright (not just unlinked) — matches
            // "finalizing is a real decision" the same way the sibling applications
            // themselves are withdrawn rather than left dangling.
            var offersToRemove = existing.Documents
                .Where(d => siblingIds.Contains(d.CourseApplicationId) && d.Category == "UniOffer")
                .ToList();
            foreach (var offer in offersToRemove)
            {
                existing.Documents.Remove(offer);
                try
                {
                    await _blobStorageService.DeleteAsync(offer.Url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Enrolment {EnrolmentId}: failed to delete blob for withdrawn offer document {DocumentId}", id, offer.Id);
                }
            }

            var auditMessage = offersToRemove.Count > 0
                ? $"Finalized a course application — all other course applications withdrawn and their {offersToRemove.Count} offer document(s) removed"
                : "Finalized a course application — all other course applications withdrawn";
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create(auditMessage, actingUserId, actingUserName));
            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Enforces sequential gating (previous step must be Complete) and, for steps that
        // require one, that a matching evidence document has already been uploaded.
        public async Task<bool> CompleteVisaStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var step = FindStep(existing, stepKey);

            // Re-completing an already-Complete step is allowed for every gated step (not
            // just VisaOutcome) — this is how "Edit details" on a completed step saves its
            // changes: the frontend reopens the fields for editing but leaves Status at
            // Complete, then calls this same endpoint again. All the per-step validation
            // below still re-runs, so a re-save can't silently skip a required document.
            // Re-running is safe for every step: fields/CompletedAt/By just get
            // overwritten, the enrolment Status recompute is idempotent, and VisaOutcome's
            // FinancialRecord creation is already idempotent too.
            if (step.Status == "Locked")
            {
                throw new ArgumentException("This step is locked — complete the previous step first.");
            }

            if (VisaProcessStepKeys.RequiredEvidenceCategory.TryGetValue(stepKey, out var requiredCategories))
            {
                var missingCategory = requiredCategories.FirstOrDefault(cat => !existing.Documents.Any(d => d.Category == cat));
                if (missingCategory != null)
                {
                    throw new ArgumentException($"Upload the required \"{missingCategory}\" evidence document before marking this step complete.");
                }
            }

            // Enrolment Form is auto-completed at enrolment creation (see
            // VisaProcessStepModel.CreateDefault), so there's no "complete this step"
            // action to gate on the service-fee invoice having been sent. The next
            // actionable step is Apply Offer, so that's where the gate actually lives —
            // staff shouldn't be able to push the process further while the fee raised on
            // the Enrolment Form step is still unbilled.
            if (stepKey == VisaProcessStepKeys.ApplyOffer)
            {
                var enrolmentFormStep = FindStep(existing, VisaProcessStepKeys.EnrolmentForm);
                var hasSentInvoice = enrolmentFormStep.Fields.TryGetValue("invoiceId", out var invoiceId) && !string.IsNullOrEmpty(invoiceId);
                if (!hasSentInvoice)
                {
                    throw new ArgumentException("Send the enrolment service-fee invoice (on the Enrolment Form step) before marking Apply Offer complete.");
                }
            }

            // A course must have been through Finalize (see FinalizeCourseApplicationAsync)
            // before staff can confirm CoE — that action is what decides which course is
            // actually going ahead and withdraws the rest, so completing CoE for a course
            // nobody has picked yet doesn't make sense.
            if (stepKey == VisaProcessStepKeys.CoeCompletion
                && !existing.CourseApplications.Any(c => c.Status == CourseApplicationStatuses.Finalized))
            {
                throw new ArgumentException("Finalize a course application before marking CoE Completion complete.");
            }

            // The whole point of this step is recording Granted/Refused — completing it
            // without one previously fell through to "treat as Refused" silently, which is
            // how an enrolment could end up VisaFail (and skip Financial record creation)
            // just because nobody picked an option.
            if (stepKey == VisaProcessStepKeys.VisaOutcome)
            {
                var outcomeValue = fields.TryGetValue("outcome", out var ov) ? ov : null;
                if (outcomeValue != "Granted" && outcomeValue != "Refused")
                {
                    throw new ArgumentException("Select an outcome (Granted or Refused) before marking this step complete.");
                }
            }

            step.Fields = fields;
            step.Status = "Complete";
            step.CompletedAt = DateTime.UtcNow;
            step.CompletedByUserId = actingUserId;
            step.CompletedByName = actingUserName;

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Completed VISA Process step \"{stepKey}\"", actingUserId, actingUserName));

            var stepIndex = Array.IndexOf(VisaProcessStepKeys.Ordered, stepKey);
            var nextKey = stepIndex >= 0 && stepIndex + 1 < VisaProcessStepKeys.Ordered.Length ? VisaProcessStepKeys.Ordered[stepIndex + 1] : null;
            if (nextKey != null)
            {
                var nextStep = FindStep(existing, nextKey);
                if (nextStep.Status == "Locked")
                {
                    nextStep.Status = "Draft";
                }
            }

            // Each gated step completion reflects directly onto the enrolment's Status —
            // the enum's own vocabulary (Offer/COE/ApplyVisa/VisaSuccess/VisaFail) mirrors
            // these four steps 1:1.
            if (stepKey == VisaProcessStepKeys.ApplyOffer)
            {
                existing.Status = EnrolmentEnums.Offer.ToString();
            }
            else if (stepKey == VisaProcessStepKeys.CoeCompletion)
            {
                existing.Status = EnrolmentEnums.Coe.ToString();
            }
            else if (stepKey == VisaProcessStepKeys.VisaApplication)
            {
                existing.Status = EnrolmentEnums.ApplyVisa.ToString();
            }
            else if (stepKey == VisaProcessStepKeys.VisaOutcome)
            {
                var outcome = fields.TryGetValue("outcome", out var o) ? o : null;
                var isGranted = outcome == "Granted";
                existing.Status = isGranted ? EnrolmentEnums.VisaSuccess.ToString() : EnrolmentEnums.VisaFail.ToString();
                existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"VISA Process complete — outcome: {outcome ?? "Unknown"}", actingUserId, actingUserName));

                if (isGranted)
                {
                    if (!string.IsNullOrEmpty(existing.StudentApplicationId))
                    {
                        await _applicationDataAccess.UpdateApplicationStatusAsync(existing.StudentApplicationId, "Studying");
                    }
                }
            }

            var completed = await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            if (completed)
            {
                await _notificationPublisher.PublishToRoleAsync(
                    module: "Enrolment",
                    entityId: id,
                    summary: $"Enrolment status changed to {existing.Status}",
                    role: SystemRole.Staff);
            }

            return completed;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Explicit status change Complete -> Draft — distinct from "Edit details" (which
        // re-runs CompleteVisaStepAsync and leaves Status at Complete). Reopening doesn't
        // touch Fields or cascade to lock any later step that was already unlocked; staff
        // already progressed there, so undoing that would be more disruptive than helpful
        // for what's meant to be a quick "fix a mistake" action.
        public async Task<bool> ReopenVisaStepAsync(string id, string stepKey, string actingUserId)
        {
            if (stepKey == VisaProcessStepKeys.StudentInfo || stepKey == VisaProcessStepKeys.EnrolmentForm)
            {
                throw new ArgumentException("This step doesn't support reopening — edit its details directly instead.");
            }

            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var step = FindStep(existing, stepKey);

            if (step.Status != "Complete")
            {
                throw new ArgumentException("Only a completed step can be reopened.");
            }

            step.Status = "Draft";
            step.CompletedAt = null;
            step.CompletedByUserId = null;
            step.CompletedByName = null;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Reopened VISA Process step \"{stepKey}\"", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // The one place the enrolment's Financial record gets created — moved here from
        // CompleteVisaStepAsync's VisaOutcome branch so "record the outcome" and "close
        // this out financially" are two distinct, explicit staff actions rather than one
        // step completion silently doing both. CreateForEnrolmentIfNotExistsAsync is
        // already idempotent, so re-finalizing an enrolment that already has a record
        // (e.g. one created under the old auto-create behaviour) is harmless.
        public async Task<bool> FinalizeEnrolmentAsync(string id, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);

            if (existing.Status == EnrolmentEnums.Finalized.ToString())
            {
                throw new ArgumentException("This enrolment has already been finalized.");
            }
            if (existing.Status != EnrolmentEnums.VisaSuccess.ToString() && existing.Status != EnrolmentEnums.VisaFail.ToString())
            {
                throw new ArgumentException("Complete the VISA Outcome step before finalizing this enrolment.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var wasGranted = existing.Status == EnrolmentEnums.VisaSuccess.ToString();

            if (wasGranted)
            {
                await _financialRecordService.CreateForEnrolmentIfNotExistsAsync(existing, actingUserId);
            }

            existing.Status = EnrolmentEnums.Finalized.ToString();
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create(
                wasGranted ? "Enrolment finalized — financial record created" : "Enrolment finalized", actingUserId, actingUserName));

            var saved = await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            if (saved)
            {
                await _notificationPublisher.PublishToRoleAsync(
                    module: "Enrolment",
                    entityId: id,
                    summary: $"Enrolment finalized for {existing.FirstName} {existing.LastName}",
                    role: SystemRole.Staff);
            }

            return saved;
        }

        // ===================== Dynamic Forms =====================

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<EnrolmentFormResponseModel> RequestFormAsync(string id, string formTemplateId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var template = await _dynamicFormTemplateDataAccess.GetByIdAsync(formTemplateId)
                ?? throw new KeyNotFoundException("Form template not found");

            if (!template.Status.Is(TemplateStatus.Active))
            {
                throw new ArgumentException("This form is inactive and cannot be requested.");
            }

            // A template can only have one "live" request on an enrolment at a time —
            // a withdrawn request must be archived (ArchiveFormResponseAsync) before the
            // same form can be requested again.
            if (existing.FormResponses.Any(r => r.FormTemplateId == formTemplateId && !r.Status.Is(ResponseStatus.Archived)))
            {
                throw new ArgumentException("This form has already been requested on this enrolment. Withdraw and archive the existing request before requesting it again.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var response = template.CreateFromTemplate(actingUserId, actingUserName);

            existing.FormResponses.Add(response);
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Requested form \"{template.Name}\" from the student", actingUserId, actingUserName));

            await SendFormEmailAsync(existing, response, "dynamic-form-request", actingUserName);
            await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            return response;
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        public async Task<bool> WithdrawFormRequestAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (response.Status.Is(ResponseStatus.Withdrawn) || response.Status.Is(ResponseStatus.Responded))
            {
                throw new ArgumentException("Only a requested or in-progress form can be withdrawn.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Withdrawn.ToString();
            response.WithdrawnAt = DateTime.UtcNow;
            response.WithdrawnByUserId = actingUserId;

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Withdrew request for form \"{response.FormName}\"", actingUserId, actingUserName));
            await SendFormEmailAsync(existing, response, "dynamic-form-withdrawn", actingUserName);

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Only a withdrawn request can be archived. Archiving is what frees up its
        // template for a fresh request — see the duplicate-request check in RequestFormAsync.
        public async Task<bool> ArchiveFormResponseAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (!response.Status.Is(ResponseStatus.Withdrawn))
            {
                throw new ArgumentException("Only a withdrawn request can be archived.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Archived.ToString();
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Archived withdrawn form \"{response.FormName}\"", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Manual override so staff can directly set a response's status when it's ended up
        // somewhere the normal guarded transitions (Withdraw/Reopen/Archive) can't fix.
        // No email — this is an administrative correction, not a workflow event the
        // student needs to hear about.
        public async Task<bool> SetFormResponseStatusAsync(string id, string responseId, string newStatus, string actingUserId)
        {
            if (!Enum.TryParse<ResponseStatus>(newStatus, out _))
            {
                throw new ArgumentException("Not a recognised form status.");
            }

            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var previousStatus = response.Status;
            response.Status = newStatus;

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Staff changed form \"{response.FormName}\" status from {previousStatus} to {newStatus}", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Reverses a finalized response back to Requesting (not Draft) and re-sends the
        // request email, so the student actually gets told to go finish it again — a
        // silent reopen gives them no way to know it needs attention.
        public async Task<bool> ReopenFormForEditAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            if (!response.Status.Is(ResponseStatus.Responded))
            {
                throw new ArgumentException("Only a finalized (Responded) form can be reopened for edit.");
            }

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Status = ResponseStatus.Requesting.ToString();

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Reopened form \"{response.FormName}\" for the student to edit", actingUserId, actingUserName));
            await SendFormEmailAsync(existing, response, "dynamic-form-request", actingUserName);

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // Staff editing a finalized response's answers directly — re-renders and re-saves
        // the PDF snapshot too, since the finalized content just changed (docs/08 §2.4).
        public async Task<bool> StaffEditFormResponseAsync(string id, string responseId, List<FormAnswerModel> answers, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);

            ValidateAnswerLengths(response, answers);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            response.Answers = answers;
            response.StaffEditedAt = DateTime.UtcNow;
            response.StaffEditedByUserId = actingUserId;

            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Edited student's answers on form \"{response.FormName}\"", actingUserId, actingUserName));

            await SaveResponseAsDocumentAsync(existing, response, actingUserId, actingUserName);

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Auth: requires EnrolmentsEdit permission + ownership — see GetOwnedEnrolmentAsync.
        // On-demand render, returned as bytes rather than a blob storage URL — a blob's
        // Content-Disposition filename hint isn't reliably honored by browsers for
        // inline-viewed PDFs (Chrome in particular falls back to the blob's own storage
        // path, which carries the collision-avoidance GUID prefix). Streaming bytes back
        // through our own controller sidesteps that entirely: the caller controls the
        // filename outright. Deliberately does NOT touch Documents/ExportedDocumentId —
        // that's the automatic save-on-submit path, see SubmitFormAsync/
        // SaveResponseAsDocumentAsync. A manual re-export shouldn't create a redundant
        // Documents entry every time staff just want a copy.
        public async Task<(byte[] Content, string FileName)> ExportFormAsync(string id, string responseId, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var response = FindFormResponse(existing, responseId);
            var studentName = $"{existing.FirstName} {existing.LastName}".Trim();

            var pdfBytes = await _pdfService.RenderToPdfAsync(response.RenderToHtml(studentName, existing.Email, existing.Mobile));
            var fileName = $"{response.FormName}-{studentName}-{DateTime.UtcNow:dd MMM yyyy h.mmtt}.pdf";
            return (pdfBytes, fileName);
        }

        // Student-facing read — no permission key, gated purely by StudentUserId
        // ownership. Used by the Applications module, which only knows its own
        // applicationId, not the linked Enrolment's id.
        public async Task<EnrolmentModel?> GetEnrolmentForStudentByApplicationIdAsync(string applicationId, string studentUserId)
        {
            var enrolment = await _enrolmentDataAccess.GetEnrolmentByStudentApplicationIdAsync(applicationId);
            return enrolment != null && enrolment.StudentUserId == studentUserId ? enrolment : null;
        }

        // Student-facing — no permission key, gated purely by StudentUserId ownership.
        // Deliberately a narrow summary (status + counts only) rather than the full
        // CourseApplications list — the individual course-shopping attempts (which were
        // tried, withdrawn, staff notes) stay staff-only; the student only needs "where am
        // I" and "how many options are in play".
        public async Task<MyEnrolmentSummaryModel?> GetMyEnrolmentSummaryAsync(string applicationId, string studentUserId)
        {
            var enrolment = await _enrolmentDataAccess.GetEnrolmentByStudentApplicationIdAsync(applicationId);
            if (enrolment == null || enrolment.StudentUserId != studentUserId) return null;

            string? finalizedName = null;
            var finalized = enrolment.CourseApplications.FirstOrDefault(c => c.Status == CourseApplicationStatuses.Finalized);
            if (finalized != null)
            {
                var partner = await _educationPartnerDataAccess.GetEducationPartnerByIdAsync(finalized.EducationPartnerId);
                var course = await _courseDataAccess.GetCourseByIdAsync(finalized.CourseId);
                var nameParts = new[] { partner?.Name, course?.CourseName }.Where(s => !string.IsNullOrEmpty(s));
                finalizedName = string.Join(" — ", nameParts);
                if (string.IsNullOrEmpty(finalizedName)) finalizedName = null;
            }

            return new MyEnrolmentSummaryModel
            {
                EnrolmentId = enrolment.Id,
                Status = enrolment.Status,
                CourseApplicationCount = enrolment.CourseApplications.Count,
                FinalizedCourseApplicationName = finalizedName
            };
        }

        // Student-facing — no permission key, gated purely by StudentUserId ownership.
        // Draft saves are the one silent transition: no email, no audit entry, since
        // it's just the student's in-progress save while gathering information.
        public async Task<bool> SaveFormDraftAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId)
        {
            var existing = await GetOwnedEnrolmentForStudentAsync(id, studentUserId);
            var response = FindEditableStudentResponse(existing, responseId);

            ValidateAnswerLengths(response, answers);

            response.Answers = answers;
            response.Status = ResponseStatus.Draft.ToString();
            response.LastSavedAt = DateTime.UtcNow;

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

        // Student-facing — no permission key, gated purely by StudentUserId ownership.
        // Validates every required question is answered, flips status to Responded,
        // and auto-saves the finalized PDF into Documents in the same call — this is
        // not an optional step, see docs/08 §2.4.
        public async Task<bool> SubmitFormAsync(string id, string responseId, List<FormAnswerModel> answers, string studentUserId)
        {
            var existing = await GetOwnedEnrolmentForStudentAsync(id, studentUserId);
            var response = FindEditableStudentResponse(existing, responseId);

            ValidateRequiredAnswers(response, answers);
            ValidateAnswerLengths(response, answers);

            response.Answers = answers;
            response.Status = ResponseStatus.Responded.ToString();
            response.SubmittedAt = DateTime.UtcNow;

            var studentName = $"{existing.FirstName} {existing.LastName}".Trim();
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create($"Student submitted form \"{response.FormName}\"", studentUserId, studentName));

            await SaveResponseAsDocumentAsync(existing, response, studentUserId, studentName);

            var saved = await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);

            if (saved)
            {
                await _notificationPublisher.PublishToRoleAsync(
                    module: "Enrolment",
                    entityId: id,
                    summary: $"{studentName} submitted \"{response.FormName}\"",
                    role: SystemRole.Staff);
            }

            return saved;
        }

        private static void ValidateRequiredAnswers(EnrolmentFormResponseModel response, List<FormAnswerModel> answers)
        {
            var missing = response.QuestionsSnapshot
                .Where(q => q.IsRequired)
                .Where(q =>
                {
                    var answer = answers.FirstOrDefault(a => a.QuestionId == q.Id);
                    if (answer == null) return true;

                    var isSelectType = q.AnswerType == AnswerType.SingleSelect.ToString() || q.AnswerType == AnswerType.MultiSelect.ToString();
                    return isSelectType ? answer.SelectedOptions.Count == 0 : string.IsNullOrWhiteSpace(answer.TextValue);
                })
                .Select(q => q.QuestionText)
                .ToList();

            if (missing.Count > 0)
            {
                throw new ArgumentException($"Please answer the following required question(s): {string.Join("; ", missing)}");
            }
        }

        // Applied to every write path (draft save, submit, staff edit) — the client-side
        // textarea maxlength is a UX nicety, this is the actual enforcement.
        private static void ValidateAnswerLengths(EnrolmentFormResponseModel response, List<FormAnswerModel> answers)
        {
            var overLimit = new List<string>();
            foreach (var question in response.QuestionsSnapshot.Where(q => q.AnswerType == AnswerType.RichText.ToString()))
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var limit = question.MaxLength ?? DynamicFormLimits.RichTextAnswerMaxLength;
                if (answer?.TextValue != null && answer.TextValue.Length > limit)
                {
                    overLimit.Add($"{question.QuestionText} (max {limit} characters)");
                }
            }

            if (overLimit.Count > 0)
            {
                throw new ArgumentException($"The following answer(s) exceed their character limit: {string.Join("; ", overLimit)}");
            }
        }

        // Steps whose bound form is that step's single canonical evidence file — mirrors
        // the frontend's VISA_STEP_EVIDENCE_CATEGORY (models/enrolment.ts) so a step-bound
        // response's auto-saved PDF lands in the same category the step's own manual-
        // upload zone reads from (documentsByCategory/stepEvidenceDocuments).
        private static readonly Dictionary<string, string> StepEvidenceCategories = new()
        {
            ["EnrolmentForm"] = "GS",
            ["ApplyOffer"] = "UniOffer",
            ["CoeCompletion"] = "CoE",
            ["VisaApplication"] = "VisaDraft",
            ["VisaOutcome"] = "VisaGranted"
        };

        // Renders the finalized response to PDF and saves it as a Documents entry.
        // A step-bound form (e.g. the GS statement) is that step's one canonical evidence
        // file — each regeneration deletes the previous blob/Documents entry and replaces
        // it, same as the step's own manual-upload zone. An un-bound ad-hoc form has no
        // single "the" evidence slot to replace, so it keeps the original append-only
        // behavior — every finalize/edit adds a new snapshot, ExportedDocumentId always
        // pointing at the latest.
        private async Task SaveResponseAsDocumentAsync(EnrolmentModel enrolment, EnrolmentFormResponseModel response, string actingUserId, string actingUserName)
        {
            var studentName = $"{enrolment.FirstName} {enrolment.LastName}".Trim();
            var pdfBytes = await _pdfService.RenderToPdfAsync(response.RenderToHtml(studentName, enrolment.Email, enrolment.Mobile));

            var evidenceCategory = response.BoundStepKey != null && StepEvidenceCategories.TryGetValue(response.BoundStepKey, out var ec) ? ec : null;
            var isStepEvidence = evidenceCategory != null;
            var category = evidenceCategory ?? "DynamicForm";

            var fileName = $"{response.FormName}-{studentName}-{DateTime.UtcNow:dd MMM yyyy h.mmtt}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            var url = await _blobStorageService.UploadAsync(stream, fileName, "application/pdf");

            var document = new EnrolmentDocumentModel
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                Category = category,
                Url = url,
                ContentType = "application/pdf",
                SizeBytes = pdfBytes.LongLength,
                UploadedByUserId = actingUserId,
                UploadedByName = actingUserName,
                IsFromStudent = false,
                UploadedAt = DateTime.UtcNow
            };

            if (isStepEvidence && response.ExportedDocumentId != null)
            {
                var previous = enrolment.Documents.FirstOrDefault(d => d.Id == response.ExportedDocumentId);
                if (previous != null)
                {
                    await _blobStorageService.DeleteAsync(previous.Url);
                    enrolment.Documents.Remove(previous);
                }
            }

            enrolment.Documents.Add(document);
            response.ExportedDocumentId = document.Id;
        }

        private async Task SendFormEmailAsync(EnrolmentModel enrolment, EnrolmentFormResponseModel response, string templateKey, string actingUserName)
        {
            var template = await _emailTemplateDataAccess.GetByKeyAsync(templateKey);
            if (template == null)
            {
                _logger.LogWarning("Dynamic Forms: email template \"{TemplateKey}\" not found — skipping notification.", templateKey);
                return;
            }

            var portalLink = $"{_frontendBaseUrl}/student-portal/application/{enrolment.StudentApplicationId}";
            var tokens = new Dictionary<string, string>
            {
                ["{{studentFirstName}}"] = enrolment.FirstName,
                ["{{formName}}"] = response.FormName,
                ["{{staffName}}"] = actingUserName,
                ["{{portalLink}}"] = portalLink
            };

            var subject = InterpolateTokens(template.Subject, tokens);
            var body = InterpolateTokens(template.Body, tokens);
            var htmlBody = body.Replace("\n", "<br/>");

            await _emailService.SendEmailAsync(enrolment.Email, subject, htmlBody, body);

            enrolment.Communications.Add(new EnrolmentCommunicationModel
            {
                TemplateKey = templateKey,
                ToEmail = enrolment.Email,
                RecipientType = "Student",
                Subject = subject,
                Body = body,
                SentByUserId = null,
                SentByName = $"{actingUserName} (via Dynamic Forms)",
                SentAt = DateTime.UtcNow
            });
        }

        private static string InterpolateTokens(string template, Dictionary<string, string> tokens)
        {
            var result = template;
            foreach (var (token, value) in tokens)
            {
                result = result.Replace(token, value);
            }
            return result;
        }

        private static EnrolmentFormResponseModel FindFormResponse(EnrolmentModel enrolment, string responseId)
        {
            return enrolment.FormResponses.FirstOrDefault(r => r.Id == responseId)
                ?? throw new KeyNotFoundException("Form response not found");
        }

        private static EnrolmentFormResponseModel FindEditableStudentResponse(EnrolmentModel enrolment, string responseId)
        {
            var response = FindFormResponse(enrolment, responseId);
            if (response.Status.Is(ResponseStatus.Withdrawn))
            {
                throw new ArgumentException("This form request was withdrawn and can no longer be edited.");
            }
            if (response.Status.Is(ResponseStatus.Responded))
            {
                throw new ArgumentException("This form has already been submitted — ask staff to reopen it before making changes.");
            }
            return response;
        }

        private async Task<EnrolmentModel> GetOwnedEnrolmentForStudentAsync(string id, string studentUserId)
        {
            var existing = await _enrolmentDataAccess.GetEnrolmentAsync(id)
                ?? throw new KeyNotFoundException("Enrolment not found");

            if (existing.StudentUserId != studentUserId)
            {
                throw new UnauthorizedAccessException("This enrolment does not belong to you.");
            }

            return existing;
        }

        // ===================== end Dynamic Forms =====================

        private static VisaProcessStepModel FindStep(EnrolmentModel enrolment, string stepKey)
        {
            if (enrolment.VisaProcessSteps.Count == 0)
            {
                enrolment.VisaProcessSteps = VisaProcessStepModel.CreateDefault(enrolment.OwnerUserId, enrolment.OwnerUserId);
            }

            return enrolment.VisaProcessSteps.FirstOrDefault(s => s.Key == stepKey)
                ?? throw new ArgumentException($"Unknown VISA Process step \"{stepKey}\"");
        }

        private async Task<EnrolmentModel> GetOwnedEnrolmentAsync(string id, string actingUserId)
        {
            // Two distinct checks: a role/permission check (can this user edit enrolments
            // at all) and an ownership check (is this THEIR enrolment specifically) — the
            // first can't tell you who owns a given record, the second can't tell you
            // whether the role is allowed to edit in the first place.
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.EnrolmentsEdit.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to edit enrolments");
            }

            var existing = await _enrolmentDataAccess.GetEnrolmentAsync(id)
                ?? throw new KeyNotFoundException("Enrolment not found");

            if (existing.OwnerUserId != actingUserId)
            {
                throw new UnauthorizedAccessException("Only the staff member who owns this enrolment can update it — ask a manager to reassign it to you first.");
            }

            return existing;
        }

        private async Task<string> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Unknown";
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId;
        }

        private static string GenerateTempPassword() => "Ef-" + Guid.NewGuid().ToString("N")[..10];
    }
}
