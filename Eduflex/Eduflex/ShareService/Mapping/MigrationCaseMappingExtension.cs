using ShareService.Models.MigrationCase;

namespace ShareService.Mapping
{
    public static class MigrationCaseMappingExtension
    {
        // Scoped to just the Customer Info tab's fields — deliberately not a general
        // ApplyEditableFields covering the whole model, since nothing else on
        // MigrationCaseModel is edited this way (steps go through CompleteStepAsync/
        // SaveStepDraftAsync, Status is service-derived, Documents/AuditTrail are
        // append-only).
        public static void ApplyCustomerInfo(this MigrationCaseModel existing, MigrationCaseModel updateModel)
        {
            existing.PrimaryContactName = updateModel.PrimaryContactName;
            existing.PrimaryContactEmail = updateModel.PrimaryContactEmail;
            existing.PrimaryContactMobile = updateModel.PrimaryContactMobile;
            existing.MiddleName = updateModel.MiddleName;
            existing.DateOfBirth = updateModel.DateOfBirth;
            existing.Gender = updateModel.Gender;
            existing.Nationality = updateModel.Nationality;
            existing.PassportNumber = updateModel.PassportNumber;
            existing.HometownAddress = updateModel.HometownAddress;
            existing.CurrentAddress = updateModel.CurrentAddress;
            existing.EmergencyContact = updateModel.EmergencyContact;
        }
    }
}
