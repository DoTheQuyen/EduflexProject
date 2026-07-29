using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
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
        private readonly IFinancialRecordService _financialRecordService;
        private readonly IValidator<EnrolmentModel> _validator;
        private readonly ILogger<EnrolmentService> _logger;
        private readonly int _documentLinkExpiryDays;

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
            IFinancialRecordService financialRecordService,
            IValidator<EnrolmentModel> validator,
            IOptions<DocumentLinkSettings> documentLinkSettings,
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
            _financialRecordService = financialRecordService;
            _validator = validator;
            _documentLinkExpiryDays = documentLinkSettings.Value.ExpiryDays;
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

            string studentUserId;
            string studentApplicationId;
            string linkAuditDescription;

            if (!string.IsNullOrWhiteSpace(existingStudentId))
            {
                // Step 1 of the New Enrolment wizard already resolved (found via search, or
                // just created) a Students collection record — link to that existing login
                // rather than creating a second one for the same person.
                var existingStudent = await _studentService.GetStudentAsync(existingStudentId, actingUserId)
                    ?? throw new KeyNotFoundException("Selected student not found");

                studentUserId = existingStudent.Student.UserId;

                var linkedApplication = new ApplicationModel
                {
                    StudentId = existingStudentId,
                    StudentName = $"{input.FirstName} {input.LastName}".Trim(),
                    StudentEmail = input.Email,
                    Description = "Course enrolment application",
                    Details = "Created by staff — attach your documents here.",
                    ApplicationType = "Enrolment",
                    DateApplied = DateTime.UtcNow,
                    Status = "Pending"
                };
                await _applicationDataAccess.CreateApplicationAsync(linkedApplication);

                studentApplicationId = linkedApplication.Id;
                linkAuditDescription = $"Linked to existing student account for {input.Email}";
            }
            else
            {
                var roles = await _roleService.GetAllRolesAsync();
                var studentRole = roles.FirstOrDefault(r => r.Name == SystemRole.Student.ToString())
                    ?? throw new InvalidOperationException("The Student role is not configured — seed the Roles collection first.");

                // 1. Create the student's login account. UserService already hashes the password,
                //    forces a change on first login and emails the temp credentials — see UserService.CreateUserAsync.
                var newUser = new UserModel
                {
                    Email = input.Email,
                    Password = GenerateTempPassword(),
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Mobile = input.Mobile,
                    RoleId = studentRole.Id
                };
                await _userService.CreateUserAsync(newUser, actingUserId);

                // 2. Create the Student profile + a starter Application record so the student can log in
                //    and immediately see something to attach documents to (existing Applications/Students infra).
                var student = new StudentModel
                {
                    UserId = newUser.Id,
                    Email = input.Email,
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Nationality = input.Nationality ?? string.Empty,
                    PassportNumber = input.PassportNumber ?? string.Empty,
                    DateOfBirth = input.DateOfBirth ?? DateTime.UtcNow,
                    PhoneNumber = input.Mobile,
                    Address = input.CurrentAddress ?? input.HometownAddress
                };
                await _applicationDataAccess.CreateStudentAsync(student);

                var newApplication = new ApplicationModel
                {
                    StudentId = student.Id,
                    StudentName = $"{input.FirstName} {input.LastName}".Trim(),
                    StudentEmail = input.Email,
                    Description = "Course enrolment application",
                    Details = "Created by staff — attach your documents here.",
                    ApplicationType = "Enrolment",
                    DateApplied = DateTime.UtcNow,
                    Status = "Pending"
                };
                await _applicationDataAccess.CreateApplicationAsync(newApplication);

                studentUserId = newUser.Id;
                studentApplicationId = newApplication.Id;
                linkAuditDescription = $"Created student user account for {input.Email}";
            }

            // 3. Create the staff-facing enrolment record that links everything together.
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

            // 4. If converted from an enquiry, flip its status so the link shows up on both sides.
            if (sourceEnquiry != null)
            {
                sourceEnquiry.Status = EnquiryEnums.Converted.ToString();
                if (string.IsNullOrWhiteSpace(sourceEnquiry.Response))
                {
                    sourceEnquiry.Response = $"Converted to enrolment #{input.Id}.";
                }
                await _enquiryService.UpdateEnquiriesAsync(sourceEnquiry.Id, sourceEnquiry, actingUserId);
            }

            _logger.LogInformation("Created enrolment {EnrolmentId} for {Email} (enquiry: {EnquiryId})", input.Id, input.Email, enquiryId ?? "none");

            return input;
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
            var permissions = await _permissionService.GetPermissionsForUserAsync(actingUserId);
            if (!permissions.Contains(PermissionKey.EnrolmentsReassign.GetDescription()))
            {
                throw new UnauthorizedAccessException("You do not have permission to reassign enrolments.");
            }

            var existing = await _enrolmentDataAccess.GetEnrolmentAsync(id)
                ?? throw new KeyNotFoundException("Enrolment not found");

            var newOwner = await _userService.GetUserByIdAsync(newOwnerUserId)
                ?? throw new ArgumentException("The selected staff member was not found");

            var previousOwnerName = await ResolveUserNameAsync(existing.OwnerUserId);
            var newOwnerName = $"{newOwner.FirstName} {newOwner.LastName}".Trim();
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            existing.OwnerUserId = newOwnerUserId;
            existing.AuditTrail.Add(EnrolmentAuditEntryModel.Create(
                $"Reassigned owner from {previousOwnerName} to {newOwnerName}", actingUserId, actingUserName));

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
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
        public async Task<bool> RenameDocumentAsync(string id, string documentId, string newFileName, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var document = existing.Documents.FirstOrDefault(d => d.Id == documentId)
                ?? throw new KeyNotFoundException("Document not found");

            var oldName = document.FileName;
            document.FileName = newFileName;

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
        // Enforces sequential gating (previous step must be Complete) and, for steps that
        // require one, that a matching evidence document has already been uploaded.
        public async Task<bool> CompleteVisaStepAsync(string id, string stepKey, Dictionary<string, string> fields, string actingUserId)
        {
            var existing = await GetOwnedEnrolmentAsync(id, actingUserId);
            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var step = FindStep(existing, stepKey);

            // VisaOutcome is the one exception to "can't re-complete a Complete step" —
            // its outcome (Granted/Refused) is exactly the kind of thing that sometimes
            // needs correcting after the fact (e.g. completed without picking one, or the
            // decision changes), and re-running this branch is safe: the enrolment status
            // just gets recomputed, and FinancialRecord creation is already idempotent.
            if (step.Status == "Complete" && stepKey != VisaProcessStepKeys.VisaOutcome)
            {
                throw new ArgumentException("This step is already complete.");
            }
            if (step.Status == "Locked")
            {
                throw new ArgumentException("This step is locked — complete the previous step first.");
            }

            if (VisaProcessStepKeys.RequiredEvidenceCategory.TryGetValue(stepKey, out var requiredCategory)
                && !existing.Documents.Any(d => d.Category == requiredCategory))
            {
                throw new ArgumentException($"Upload the required \"{requiredCategory}\" evidence document before marking this step complete.");
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

                    await _financialRecordService.CreateForEnrolmentIfNotExistsAsync(existing, actingUserId);
                }
            }

            return await _enrolmentDataAccess.ReplaceEnrolmentAsync(id, existing);
        }

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
