using ShareService.DataAccess.Interface;
using ShareService.Enums;
using ShareService.Enums.Permissions;
using ShareService.Models.StudentPaymentPlan;
using ShareService.Services.Interface;

namespace ShareService.Services
{
    public class StudentPaymentPlanService : IStudentPaymentPlanService
    {
        private readonly IStudentPaymentPlanEntry _dataAccess;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;

        public StudentPaymentPlanService(IStudentPaymentPlanEntry dataAccess, IPermissionService permissionService, IUserService userService)
        {
            _dataAccess = dataAccess;
            _permissionService = permissionService;
            _userService = userService;
        }

        private async Task RequirePermissionAsync(string userId, PermissionKey key, string action)
        {
            var permissions = await _permissionService.GetPermissionsForUserAsync(userId);
            if (!permissions.Contains(key.GetDescription()))
            {
                throw new UnauthorizedAccessException($"You do not have permission to {action}");
            }
        }

        private async Task<string> ResolveUserNameAsync(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId;
        }

        public async Task<List<StudentPaymentPlanEntryModel>> GetByEnrolmentIdAsync(string enrolmentId, string userId)
        {
            await RequirePermissionAsync(userId, PermissionKey.EnrolmentsView, "view payment plans");
            return await _dataAccess.GetByEnrolmentIdAsync(enrolmentId);
        }

        public async Task<List<StudentPaymentPlanEntryModel>> GeneratePlanAsync(
            string enrolmentId, string studentName, string? courseName, string feeType,
            decimal totalAmount, int instalmentCount, DateTime firstDueDate, int intervalMonths, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "set up payment plans");

            if (instalmentCount < 1)
            {
                throw new ArgumentException("A payment plan needs at least one instalment.");
            }

            // Scoped per fee type, not per enrolment — a student can have a Tuition plan
            // and, later, a separate Visa485 plan without the second generate call being
            // blocked by the first plan's existence.
            var existing = (await _dataAccess.GetByEnrolmentIdAsync(enrolmentId))
                .Where(e => e.FeeType == feeType).ToList();
            if (existing.Count > 0)
            {
                throw new ArgumentException($"This enrolment already has a {feeType} payment plan — add or edit instalments individually instead of regenerating.");
            }

            // Split evenly in cents, then push whatever's left over (from rounding) onto
            // the final instalment so the entries always sum exactly to totalAmount.
            var baseShare = Math.Floor(totalAmount / instalmentCount * 100m) / 100m;
            var remainder = totalAmount - (baseShare * instalmentCount);

            var actingUserName = await ResolveUserNameAsync(actingUserId);
            var entries = new List<StudentPaymentPlanEntryModel>();
            for (var i = 1; i <= instalmentCount; i++)
            {
                var isLast = i == instalmentCount;
                entries.Add(new StudentPaymentPlanEntryModel
                {
                    EnrolmentId = enrolmentId,
                    StudentName = studentName,
                    CourseName = courseName,
                    FeeType = feeType,
                    Label = $"Instalment {i} of {instalmentCount}",
                    InstalmentNumber = i,
                    TotalInstalments = instalmentCount,
                    DueDate = firstDueDate.AddMonths(intervalMonths * (i - 1)),
                    Amount = isLast ? baseShare + remainder : baseShare,
                    Status = StudentPaymentPlanEntryStatuses.Planned,
                    IsManual = false,
                    CreatedByUserId = actingUserId,
                    CreatedByName = actingUserName
                });
            }

            await _dataAccess.CreateManyAsync(entries);
            return entries;
        }

        public async Task<StudentPaymentPlanEntryModel> AddManualEntryAsync(
            string enrolmentId, string studentName, string? courseName, string feeType, string label,
            decimal amount, DateTime dueDate, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "add payment plan instalments");

            var existingOfType = (await _dataAccess.GetByEnrolmentIdAsync(enrolmentId))
                .Where(e => e.FeeType == feeType).ToList();
            var nextNumber = existingOfType.Count + 1;
            var actingUserName = await ResolveUserNameAsync(actingUserId);

            var entry = new StudentPaymentPlanEntryModel
            {
                EnrolmentId = enrolmentId,
                StudentName = studentName,
                CourseName = courseName,
                FeeType = feeType,
                Label = label,
                InstalmentNumber = nextNumber,
                TotalInstalments = nextNumber,
                DueDate = dueDate,
                Amount = amount,
                Status = StudentPaymentPlanEntryStatuses.Planned,
                IsManual = true,
                CreatedByUserId = actingUserId,
                CreatedByName = actingUserName
            };

            await _dataAccess.CreateAsync(entry);

            // TotalInstalments on earlier entries of the same fee type is now stale —
            // kept as a display snapshot rather than a live count, same trade-off
            // InvoiceModel makes for its other snapshot fields; Account Timeline
            // recomputes "N of M" from the full list length when it needs an accurate
            // count.
            return entry;
        }

        public async Task<StudentPaymentPlanEntryModel> UpdateEntryDateAsync(string entryId, DateTime dueDate, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "edit payment plan instalments");

            var entry = await _dataAccess.GetByIdAsync(entryId)
                ?? throw new KeyNotFoundException("Payment plan instalment not found");
            if (entry.Status != StudentPaymentPlanEntryStatuses.Planned)
            {
                throw new ArgumentException("Only a Planned instalment's due date can be changed.");
            }

            entry.DueDate = dueDate;
            await _dataAccess.ReplaceAsync(entryId, entry);
            return entry;
        }

        public async Task<StudentPaymentPlanEntryModel> SkipEntryAsync(string entryId, string? reason, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "skip payment plan instalments");

            var entry = await _dataAccess.GetByIdAsync(entryId)
                ?? throw new KeyNotFoundException("Payment plan instalment not found");
            if (entry.Status != StudentPaymentPlanEntryStatuses.Planned)
            {
                throw new ArgumentException("Only a Planned instalment can be skipped.");
            }

            entry.Status = StudentPaymentPlanEntryStatuses.Skipped;
            entry.SkipReason = reason;
            await _dataAccess.ReplaceAsync(entryId, entry);
            return entry;
        }

        public async Task<StudentPaymentPlanEntryModel> RestoreEntryAsync(string entryId, string actingUserId)
        {
            await RequirePermissionAsync(actingUserId, PermissionKey.EnrolmentsEdit, "restore payment plan instalments");

            var entry = await _dataAccess.GetByIdAsync(entryId)
                ?? throw new KeyNotFoundException("Payment plan instalment not found");
            if (entry.Status != StudentPaymentPlanEntryStatuses.Skipped)
            {
                throw new ArgumentException("Only a Skipped instalment can be restored.");
            }

            entry.Status = StudentPaymentPlanEntryStatuses.Planned;
            entry.SkipReason = null;
            await _dataAccess.ReplaceAsync(entryId, entry);
            return entry;
        }

        public async Task MarkEntryInvoicedAsync(string entryId, string invoiceId)
        {
            var entry = await _dataAccess.GetByIdAsync(entryId);
            if (entry == null) return;

            entry.Status = StudentPaymentPlanEntryStatuses.Invoiced;
            entry.LinkedInvoiceId = invoiceId;
            await _dataAccess.ReplaceAsync(entryId, entry);
        }
    }
}
