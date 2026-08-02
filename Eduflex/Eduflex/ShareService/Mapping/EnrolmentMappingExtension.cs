using ShareService.Models.Application;
using ShareService.Models.Auth;
using ShareService.Models.Enrolment;
using ShareService.Models.Student;

namespace ShareService.Mapping
{
    // Model-to-model mapping used by EnrolmentService — kept separate from the
    // orchestration logic (permission checks, persistence calls, audit trail) so object-literal
    // construction and field-copy updates don't bury the actual control flow.
    public static class EnrolmentMappingExtension
    {
        public static UserModel ToNewUserModel(this EnrolmentModel input, string roleId, string tempPassword)
        {
            return new UserModel
            {
                Email = input.Email,
                Password = tempPassword,
                FirstName = input.FirstName,
                LastName = input.LastName,
                Mobile = input.Mobile,
                RoleId = roleId
            };
        }

        public static StudentModel ToStudentModel(this EnrolmentModel input, string userId)
        {
            return new StudentModel
            {
                UserId = userId,
                Email = input.Email,
                FirstName = input.FirstName,
                LastName = input.LastName,
                Nationality = input.Nationality ?? string.Empty,
                PassportNumber = input.PassportNumber ?? string.Empty,
                DateOfBirth = input.DateOfBirth ?? DateTime.UtcNow,
                PhoneNumber = input.Mobile,
                Address = input.CurrentAddress ?? input.HometownAddress
            };
        }

        // Shared by both the "existing student" and "new student" branches of
        // EnrolmentService.CreateInternalAsync — previously duplicated almost verbatim in
        // each branch; the only real difference between them is which studentId to use.
        public static ApplicationModel ToEnrolmentApplicationModel(this EnrolmentModel input, string studentId)
        {
            return new ApplicationModel
            {
                StudentId = studentId,
                StudentName = $"{input.FirstName} {input.LastName}".Trim(),
                StudentEmail = input.Email,
                Description = "Course enrolment application",
                Details = "Created by staff — attach your documents here.",
                ApplicationType = "Enrolment",
                DateApplied = DateTime.UtcNow,
                Status = "Pending"
            };
        }

        // Overwrites only the staff-editable Student Information + Enrolment Form fields.
        // Ownership/linkage fields and the append-only sub-collections are never touched here —
        // they're mutated through their own dedicated service methods so every change is audited.
        public static void ApplyEditableFields(this EnrolmentModel existing, EnrolmentModel updateModel)
        {
            existing.FirstName = updateModel.FirstName;
            existing.MiddleName = updateModel.MiddleName;
            existing.LastName = updateModel.LastName;
            existing.DateOfBirth = updateModel.DateOfBirth;
            existing.Gender = updateModel.Gender;
            existing.Email = updateModel.Email;
            existing.Mobile = updateModel.Mobile;
            existing.Nationality = updateModel.Nationality;
            existing.PassportNumber = updateModel.PassportNumber;
            existing.HometownAddress = updateModel.HometownAddress;
            existing.CurrentAddress = updateModel.CurrentAddress;
            existing.EmergencyContact = updateModel.EmergencyContact;

            existing.EducationPartnerId = updateModel.EducationPartnerId;
            existing.CourseId = updateModel.CourseId;
            existing.Intake = updateModel.Intake;
            existing.StudyMode = updateModel.StudyMode;
            existing.Campus = updateModel.Campus;
            existing.CommencementDate = updateModel.CommencementDate;
            existing.ActualCommencementDate = updateModel.ActualCommencementDate;
            existing.ExpectedCompletionDate = updateModel.ExpectedCompletionDate;
            existing.FundingSource = updateModel.FundingSource;
            existing.VisaStatus = updateModel.VisaStatus;
            existing.Status = updateModel.Status;
            existing.Notes = updateModel.Notes;
            existing.TuitionFee = updateModel.TuitionFee;
        }
    }
}
