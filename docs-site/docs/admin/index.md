# Admin guide

This guide covers the **Administration** and **Template** sections of the Staff
Portal. These menus require the Admin role.

## Set-up order for a new organisation

Do these in order — later steps depend on earlier ones.

1. [Create a role](roles/create-a-role.md) for each kind of staff member.
2. [Assign permissions](users/assign-permissions.md) to each role.
3. [Manage departments](departments/manage-departments.md) and add members.
4. [Add a user](users/add-a-user.md) for each staff member and give them a role.
5. [Create a form template](templates/create-a-form-template.md) for each form
   you collect from students.
6. [Manage practitioner tags](templates/manage-practitioner-tags.md), then
   [configure a visa process template](templates/configure-a-visa-process-template.md)
   for each visa category you handle.
7. [Manage email templates](templates/manage-email-templates.md) and
   [invoice templates](templates/manage-invoice-templates.md).
8. Review [App settings](settings/app-settings.md).

## Reference

- [Permission matrix](../reference/permission-matrix.md) — what each permission
  key unlocks.
- [Statuses](../reference/statuses.md) — every status value and what it means.

::: warning Permissions are applied at sign-in
Changing a role's permissions does not affect users who are already signed
in. They must sign out and back in before the change takes effect.
:::
