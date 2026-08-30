# Permission matrix

Permissions are granted to **roles**, not to individual users. Each key below
unlocks a screen or an action. A user with none of a module's keys does not see
that module in the sidebar at all.

::: warning Applied at sign-in
A user picks up permission changes only when they next sign in. See
[Assign permissions](../admin/users/assign-permissions.md).
:::

## Reading the table

Most modules use the same four-key shape:

| Suffix | Allows |
|---|---|
| `View` | Open the module and read its records. |
| `Add` | Create a new record. |
| `Edit` | Change an existing record. |
| `Delete` | Remove a record. |

Low-frequency configuration screens use a **single flat key** instead, because
they are one-time set-up screens edited by one audience rather than a
multi-role workflow.

## Operational modules

| Module | Keys |
|---|---|
| Applications | `ApplicationsView` `ApplicationsAdd` `ApplicationsEdit` `ApplicationsDelete` |
| Enquiries | `EnquiryView` `EnquiryAdd` `EnquiryEdit` `EnquiryDelete` |
| Students | `StudentsView` `StudentsAdd` `StudentsEdit` `StudentsDelete` |
| Enrolments | `EnrolmentsView` `EnrolmentsAdd` `EnrolmentsEdit` `EnrolmentsDelete` `EnrolmentsReassign` |
| Migration cases | `MigrationCasesView` `MigrationCasesAdd` `MigrationCasesEdit` `MigrationCasesDelete` `MigrationCasesReassign` |
| Education partners | `EducationPartnersView` `EducationPartnersAdd` `EducationPartnersEdit` `EducationPartnersDelete` |
| Business partners | `BusinessPartnersView` `BusinessPartnersAdd` `BusinessPartnersEdit` `BusinessPartnersDelete` |
| Finance | `FinanceView` `FinanceAdd` `FinanceEdit` `FinanceDelete` |
| Course promotions | `CoursePromotionsView` `CoursePromotionsAdd` `CoursePromotionsEdit` `CoursePromotionsDelete` |
| Feedback | `FeedbackView` `FeedbackAdd` `FeedbackEdit` `FeedbackDelete` |

## Tasks

| Key | Allows |
|---|---|
| `TasksView` `TasksAdd` `TasksEdit` | Use the Tasks feature. Every staff member normally gets all three — **My Tasks** is available to anyone who can be an assigner or assignee. |
| `TasksViewAll` | See the department-scoped **All Tasks** page. Manager and Admin only. |

Responding to a task, adding a note, reassigning it and completing or reopening
it are **not** separate keys. They are ownership checks: what you can do to a
specific task depends on whether you are its assigner or its current assignee.
That split — *the key says you can use the feature, ownership says what you can
do to this record* — is used across the system, not just in Tasks.

## Administration and configuration

| Key | Gates |
|---|---|
| `UsersView` `UsersAdd` `UsersEdit` `UsersDelete` | The **Users** screen. |
| `RolesView` `RolesAdd` `RolesEdit` `RolesDelete` | The **Roles** screen, including permission assignment. |
| `DepartmentsView` `DepartmentsAdd` `DepartmentsEdit` `DepartmentsDelete` | The **Departments** screen. |
| `SettingsEdit` | The **App Settings** screen. |
| `DynamicFormsEdit` | Form template management. |
| `VisaProcessTemplatesEdit` | Both **VISA Process Templates** and **Practitioner Tags**. |
| `EmailTemplatesEdit` | Creating, updating and deactivating email templates. Reading the template list is ungated — any staff member composing an email needs it. |
| `InvoiceTemplatesEdit` | The invoice template screen and the sent-invoice ledger. |

## Invoice actions are routed by recipient, not by template key

Sending, resending, cancelling or confirming a specific invoice does **not**
use `InvoiceTemplatesEdit`. The required key depends on who the invoice is
addressed to:

| Recipient | Key checked |
|---|---|
| Student | `EnrolmentsEdit` — the action happens inside the Enrolments module. |
| Education partner, business partner, custom | `FinanceEdit` — a Finance-module action. |

## See also

- [Create a role](../admin/roles/create-a-role.md)
- [Assign permissions](../admin/users/assign-permissions.md)
