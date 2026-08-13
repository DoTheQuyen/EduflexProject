using System.Text.Json.Serialization;

namespace ShareService.Enums.Permissions
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionKey
    {
        ApplicationsView,
        ApplicationsAdd,
        ApplicationsEdit,
        ApplicationsDelete,

        FinanceView,
        FinanceAdd,
        FinanceEdit,
        FinanceDelete,

        CoursePromotionsView,
        CoursePromotionsAdd,
        CoursePromotionsEdit,
        CoursePromotionsDelete,

        RolesView,
        RolesAdd,
        RolesEdit,
        RolesDelete,

        UsersView,
        UsersAdd,
        UsersEdit,
        UsersDelete,

        EnquiryView,
        EnquiryAdd,
        EnquiryEdit,
        EnquiryDelete,

        EducationPartnersView,
        EducationPartnersAdd,
        EducationPartnersEdit,
        EducationPartnersDelete,

        BusinessPartnersView,
        BusinessPartnersAdd,
        BusinessPartnersEdit,
        BusinessPartnersDelete,

        EnrolmentsView,
        EnrolmentsAdd,
        EnrolmentsEdit,
        EnrolmentsDelete,
        EnrolmentsReassign,

        FeedbackView,
        FeedbackAdd,
        FeedbackEdit,
        FeedbackDelete,

        StudentsView,
        StudentsAdd,
        StudentsEdit,
        StudentsDelete,

        SettingsEdit,

        DepartmentsView,
        DepartmentsAdd,
        DepartmentsEdit,
        DepartmentsDelete,

        // Tasks: View/Add/Edit gate the feature generally (every staff member gets all
        // three — My Tasks is available to any assigner/assignee). Responding, adding a
        // note, reassigning and completing/reopening a task are NOT separate keys — those
        // are ownership checks inside TaskItemService (are you the assigner or the current
        // assignee), the same "permission key = can use the feature at all, ownership =
        // what you can do to THIS record" split EnrolmentService uses. TasksViewAll is the
        // separate, Manager/Admin-only key for the department-scoped All Tasks page (see
        // migration 043) — a Staff member never sees tasks that aren't theirs.
        TasksView,
        TasksAdd,
        TasksEdit,
        TasksViewAll,

        // Single flat key, same shape as SettingsEdit — Dynamic Forms template
        // management is a low-frequency, one-time-config admin screen, not a frequent
        // multi-role workflow, so it doesn't get a full View/Add/Edit/Delete set.
        DynamicFormsEdit,

        // Same shape as DynamicFormsEdit — gates both the VISA Process Templates screen
        // (VisaProcessTemplateModel catalog) and the Practitioner Tags screen
        // (PractitionerTagModel catalog). One key for both rather than a second one for
        // Practitioner Tags specifically — that catalog is small and config-adjacent to the
        // process templates themselves, edited by the same admin audience; see
        // docs/09-visa-process-config-module-design.md §C.9 for why a dedicated key there
        // would be permission sprawl for a catalog this size.
        VisaProcessTemplatesEdit,

        // Full View/Add/Edit/Delete/Reassign set, same shape as Enrolments* — a
        // MigrationCase is a sibling operational entity to Enrolment (ownership,
        // multi-step process, reassignment), not a low-frequency admin config screen, so
        // it doesn't get the single-flat-key DynamicFormsEdit/VisaProcessTemplatesEdit
        // shape. MigrationCasesDelete is seeded but not yet wired to any action, matching
        // EnrolmentsDelete's own precedent in this codebase.
        MigrationCasesView,
        MigrationCasesAdd,
        MigrationCasesEdit,
        MigrationCasesDelete,
        MigrationCasesReassign,

        // Same shape as DynamicFormsEdit — admin-only template catalog screen.
        // GetAll stays ungated (any staff member composing an email needs the list),
        // only the manage operations (create/update/deactivate) require this key.
        EmailTemplatesEdit,

        // Same shape as EmailTemplatesEdit — gates the admin Invoice Template management
        // screen and the sent-invoice ledger view. Granted to Admin and Manager (see
        // migration 040) — Manager already owns the rest of the Finance workflow via
        // FinanceEdit below, so it shouldn't be Admin-only here.
        //
        // Sending/resending/cancelling/confirming a specific invoice does NOT use this
        // key — InvoiceService.RequireInvoiceActionPermissionAsync routes each action by
        // the invoice's RecipientType instead: Student invoices check EnrolmentsEdit
        // (that action happens inside the Enrolments module), EducationPartner/
        // BusinessPartner/Custom invoices check FinanceEdit (a Finance-module action).
        InvoiceTemplatesEdit,
    }
}