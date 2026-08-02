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

        // Single flat key, same shape as SettingsEdit — Dynamic Forms template
        // management is a low-frequency, one-time-config admin screen, not a frequent
        // multi-role workflow, so it doesn't get a full View/Add/Edit/Delete set.
        DynamicFormsEdit,

        // Same shape as DynamicFormsEdit — admin-only template catalog screen.
        // GetAll stays ungated (any staff member composing an email needs the list),
        // only the manage operations (create/update/deactivate) require this key.
        EmailTemplatesEdit,
    }
}