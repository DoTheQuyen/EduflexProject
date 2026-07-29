using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShareService.Common;
using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Enums.Roles;
using ShareService.Models.Auth;
using ShareService.Models.Setting;
using ShareService.Models.Student;
using ShareService.Services.Interface;
using ShareService.Services.Interface.Integration;
using ShareService.Services.Service.Integration;
using System.Security.Cryptography;

namespace ShareService.Services
{
    public class StudentService : IStudentService
    {
        private readonly IApplication _applicationDataAccess;
        private readonly IStudentDB _studentDB;
        private readonly IUserDB _userDB;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IAzureEmailService _emailService;
        private readonly IValidator<StudentModel> _validator;
        private readonly WebURLSettings _appSettings;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            IApplication applicationDataAccess,
            IStudentDB studentDB,
            IUserDB userDB,
            IUserService userService,
            IRoleService roleService,
            IPermissionService permissionService,
            IAzureEmailService emailService,
            IValidator<StudentModel> validator,
            IOptions<WebURLSettings> appSettings,
            ILogger<StudentService> logger)
        {
            _applicationDataAccess = applicationDataAccess;
            _studentDB = studentDB;
            _userDB = userDB;
            _userService = userService;
            _roleService = roleService;
            _permissionService = permissionService;
            _emailService = emailService;
            _validator = validator;
            _appSettings = appSettings.Value;
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

        // Auth: none — self-service "my own profile", any authenticated user, always
        // acting on their own account (userId from the token, resolved by StudentsController).
        public async Task<StudentModel?> GetMyProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty");

            var student = await _applicationDataAccess.GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                _logger.LogInformation("No student profile found for user {UserId}", userId);
            }
            return student;
        }

        // Auth: requires StudentsView permission (staff-only).
        public async Task<PagedResult<StudentAccountModel>> SearchStudentsAsync(StudentFilter filter, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsView, "view students");

            List<string>? restrictToUserIds = null;
            if (filter.IsActive.HasValue)
            {
                restrictToUserIds = await _userDB.GetUserIdsByActiveStatusAsync(filter.IsActive.Value);
            }

            var paged = await _studentDB.SearchAsync(filter, restrictToUserIds);

            // Some Student records may predate this feature (or come from legacy test
            // data) and have no paired User yet — guard the lookup rather than letting
            // a null/empty UserId blow up the dictionary.
            var userIds = paged.Items.Select(s => s.UserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var users = await _userDB.GetUsersByIdsAsync(userIds);
            var userById = users.ToDictionary(u => u.Id);

            var items = paged.Items
                .Select(s => BuildAccountModel(s, string.IsNullOrEmpty(s.UserId) ? null : userById.GetValueOrDefault(s.UserId)))
                .ToList();

            return new PagedResult<StudentAccountModel>
            {
                Items = items,
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize
            };
        }

        // Auth: requires StudentsView permission (staff-only).
        public async Task<StudentAccountModel?> GetStudentAsync(string id, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsView, "view students");

            var student = await _studentDB.GetByIdAsync(id);
            if (student == null) return null;

            var user = await _userDB.GetUserByIdAsync(student.UserId);
            return BuildAccountModel(student, user);
        }

        // Auth: requires StudentsAdd permission — used by the "add student" form to
        // check email/mobile/DOB/passport before submitting, so staff can be offered
        // "reactivate this record" instead of hitting a duplicate error on create.
        public async Task<DuplicateCheckResult> CheckDuplicateAsync(string email, string mobile, DateTime dateOfBirth, string passportNumber, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsAdd, "add students");
            return await FindDuplicateAsync(email, mobile, dateOfBirth, passportNumber, excludeStudentId: null);
        }

        private async Task<DuplicateCheckResult> FindDuplicateAsync(string email, string mobile, DateTime dateOfBirth, string passportNumber, string? excludeStudentId)
        {
            var userByEmail = await _userDB.GetUserByEmailAsync(email);
            if (userByEmail != null)
            {
                var match = await BuildResultForUserMatchAsync("Email", userByEmail, excludeStudentId);
                if (match != null) return match;
            }

            var userByMobile = await _userDB.GetUserByMobileAsync(mobile);
            if (userByMobile != null)
            {
                var match = await BuildResultForUserMatchAsync("Mobile", userByMobile, excludeStudentId);
                if (match != null) return match;
            }

            var studentByPassport = await _studentDB.GetByPassportNumberAsync(passportNumber);
            if (studentByPassport != null && studentByPassport.Id != excludeStudentId)
            {
                return await BuildResultForStudentMatchAsync("PassportNumber", studentByPassport);
            }

            var studentByDob = await _studentDB.GetByDateOfBirthAsync(dateOfBirth);
            if (studentByDob != null && studentByDob.Id != excludeStudentId)
            {
                return await BuildResultForStudentMatchAsync("DateOfBirth", studentByDob);
            }

            return new DuplicateCheckResult { IsDuplicate = false };
        }

        private async Task<DuplicateCheckResult?> BuildResultForUserMatchAsync(string field, UserModel matchedUser, string? excludeStudentId)
        {
            var pairedStudent = await _studentDB.GetByUserIdAsync(matchedUser.Id);
            if (pairedStudent != null && pairedStudent.Id == excludeStudentId) return null;

            return new DuplicateCheckResult
            {
                IsDuplicate = true,
                MatchedField = field,
                ExistingUserId = matchedUser.Id,
                ExistingStudentId = pairedStudent?.Id,
                ExistingIsActive = matchedUser.IsActive
            };
        }

        private async Task<DuplicateCheckResult> BuildResultForStudentMatchAsync(string field, StudentModel matchedStudent)
        {
            var pairedUser = await _userDB.GetUserByIdAsync(matchedStudent.UserId);
            return new DuplicateCheckResult
            {
                IsDuplicate = true,
                MatchedField = field,
                ExistingUserId = matchedStudent.UserId,
                ExistingStudentId = matchedStudent.Id,
                ExistingIsActive = pairedUser?.IsActive ?? false
            };
        }

        // Auth: requires StudentsAdd permission, PLUS — because this method also
        // creates the student's login account — the caller effectively needs UsersAdd
        // too (checked inside UserService.CreateUserAsync below). Same composite-
        // permission pattern EnrolmentService already uses for the same reason.
        public async Task<StudentAccountModel> CreateStudentAsync(UserModel newUser, StudentModel studentProfile, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsAdd, "add students");

            var validation = await _validator.ValidateAsync(studentProfile);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var duplicate = await FindDuplicateAsync(newUser.Email, newUser.Mobile, studentProfile.DateOfBirth, studentProfile.PassportNumber, excludeStudentId: null);
            if (duplicate.IsDuplicate)
            {
                throw duplicate.ExistingIsActive
                    ? new ArgumentException($"A student with this {duplicate.MatchedField} already exists")
                    : new ArgumentException($"An inactive student with this {duplicate.MatchedField} already exists — reactivate that record instead of creating a new one");
            }

            var roles = await _roleService.GetAllRolesAsync();
            var studentRole = roles.FirstOrDefault(r => r.Name == SystemRole.Student.ToString())
                ?? throw new InvalidOperationException("The Student role is not configured — seed the Roles collection first.");

            newUser.Id = string.Empty;
            newUser.RoleId = studentRole.Id;
            await _userService.CreateUserAsync(newUser, actingUserId);

            var actingUserName = await ResolveUserNameAsync(actingUserId);

            studentProfile.Id = string.Empty;
            studentProfile.UserId = newUser.Id;
            studentProfile.Email = newUser.Email;
            studentProfile.PhoneNumber = newUser.Mobile;
            studentProfile.AuditTrail = new List<StudentAuditEntryModel>
            {
                StudentAuditEntryModel.Create("Student account created", actingUserId, actingUserName)
            };

            var created = await _studentDB.CreateAsync(studentProfile);

            _logger.LogInformation("Created student {StudentId} for user {UserId}", created.Id, newUser.Id);

            return new StudentAccountModel { Student = created, Mobile = newUser.Mobile, IsActive = true, LastLogin = null };
        }

        // Auth: requires StudentsEdit permission. Email/mobile changes update the
        // paired User record directly (via IUserDB) rather than going through
        // UserService.UpdateUserAsync — that method requires UsersEdit (Admin-only,
        // see migration 013) and does a full role/escalation-checked replace, which
        // is more than a contact-info sync needs. Student lifecycle management stays
        // gated by StudentsEdit throughout, consistent with the rest of this service.
        public async Task<StudentAccountModel> UpdateStudentAsync(string id, string email, string mobile, StudentModel profileUpdates, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsEdit, "update students");

            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(mobile)) throw new ArgumentException("Mobile is required");

            var validation = await _validator.ValidateAsync(profileUpdates);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                throw new ArgumentException($"Validation failed: {errors}");
            }

            var existing = await _studentDB.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Student not found");

            var existingUser = await _userDB.GetUserByIdAsync(existing.UserId)
                ?? throw new KeyNotFoundException("Paired user account not found");

            var duplicate = await FindDuplicateAsync(email, mobile, profileUpdates.DateOfBirth, profileUpdates.PassportNumber, excludeStudentId: id);
            if (duplicate.IsDuplicate)
            {
                throw new ArgumentException($"Another student already uses this {duplicate.MatchedField}");
            }

            if (email != existingUser.Email || mobile != existingUser.Mobile)
            {
                await _userDB.UpdateContactInfoAsync(existingUser.Id, email, mobile);
            }

            existing.FirstName = profileUpdates.FirstName;
            existing.LastName = profileUpdates.LastName;
            existing.Nationality = profileUpdates.Nationality;
            existing.PassportNumber = profileUpdates.PassportNumber;
            existing.DateOfBirth = profileUpdates.DateOfBirth;
            existing.Address = profileUpdates.Address;
            existing.Email = email;
            existing.PhoneNumber = mobile;

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(StudentAuditEntryModel.Create("Updated student details", actingUserId, actingUserName));

            await _studentDB.UpdateAsync(id, existing);

            return new StudentAccountModel { Student = existing, Mobile = mobile, IsActive = existingUser.IsActive, LastLogin = existingUser.LastLogin };
        }

        // Auth: requires StudentsDelete permission — deactivation is this module's
        // soft-delete, mirroring how other modules treat their Delete permission.
        public async Task<bool> DeactivateStudentAsync(string id, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsDelete, "deactivate students");

            var existing = await _studentDB.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Student not found");

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(StudentAuditEntryModel.Create("Student deactivated", actingUserId, actingUserName));
            await _studentDB.UpdateAsync(id, existing);

            return await _userDB.SetActiveStatusAsync(existing.UserId, false);
        }

        // Auth: requires StudentsEdit permission — restoring an archived record is
        // treated as an edit, not a delete-class action.
        public async Task<bool> ReactivateStudentAsync(string id, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsEdit, "reactivate students");

            var existing = await _studentDB.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Student not found");

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(StudentAuditEntryModel.Create("Student reactivated", actingUserId, actingUserName));
            await _studentDB.UpdateAsync(id, existing);

            return await _userDB.SetActiveStatusAsync(existing.UserId, true);
        }

        // Auth: requires StudentsEdit permission.
        public async Task SendPasswordResetAsync(string id, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.StudentsEdit, "send password resets");

            var existing = await _studentDB.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Student not found");

            var user = await _userDB.GetUserByIdAsync(existing.UserId)
                ?? throw new KeyNotFoundException("Paired user account not found");

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var expiry = DateTime.UtcNow.AddHours(1);
            await _userDB.SetPasswordResetTokenAsync(user.Id, token, expiry);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            existing.AuditTrail.Add(StudentAuditEntryModel.Create("Password reset email sent", actingUserId, actingUserName));
            await _studentDB.UpdateAsync(id, existing);

            var resetLink = $"{_appSettings.FrontendBaseUrl}/reset-password?token={token}";
            var (subject, html, plainText) = EmailTemplates.PasswordReset(user.FirstName, resetLink);
            await _emailService.SendEmailAsync(user.Email, subject, html, plainText);
        }

        private static StudentAccountModel BuildAccountModel(StudentModel student, UserModel? user)
        {
            return new StudentAccountModel
            {
                Student = student,
                Mobile = user?.Mobile ?? string.Empty,
                IsActive = user?.IsActive ?? false,
                LastLogin = user?.LastLogin
            };
        }
    }
}
