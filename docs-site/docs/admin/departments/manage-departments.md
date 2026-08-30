# Manage departments

**Who can do this:** Admin, Manager (`DepartmentsView` / `DepartmentsAdd` / `DepartmentsEdit` / `DepartmentsDelete`)
**Where:** Staff Portal → **Administration** → **Departments**
**Before you start:** The staff you want as members must already exist as users
— see [Add a user](../users/add-a-user.md).

## The Departments list

| Column | Shows |
|---|---|
| Name | |
| Description | Hidden on laptop-width screens and below. |
| Parent | The parent department's name, or blank if top-level. |

Search with *Search department name…*. The list is paged server-side. An empty
list shows *No departments yet. Click "Add Department" to create one.*

**Add Department** appears only if your role has the departments-add permission.

## Add a department

1. Select **Add Department**.
2. Complete the fields below.
3. Select **Save Department**.

### Name

Required, maximum 150 characters.

### Description

Optional, maximum 300 characters.

### Parent department

Optional. Choose **None (top-level)** or another department to nest this one
underneath it. Departments can therefore form a hierarchy rather than a flat
list.

When editing, the department itself is greyed out in this dropdown — a
department cannot be its own parent.

### Members

A checklist of every staff user, each shown as *First Last - Role: RoleName*.
Tick everyone who belongs to the department.

If no staff exist yet, the list shows *No staff available yet.*

### Department head

A dropdown listing **only the members you have ticked**, plus **None**. The
note under the field says: *Only selected members can be assigned as head.*

::: tip Order matters here
Tick the members first, then choose the head. Someone who is not a ticked
member will not appear in the head dropdown.
:::

## Edit or delete a department

Opening an existing department shows the same dialog titled **Edit
Department**, and the save button reads **Save Changes**.

Delete lives **inside the edit dialog**, not as a row button, and has its own
confirmation step. It appears only if your role has the departments-delete
permission.

## What a department affects

Department membership scopes the **All Tasks** page: a Manager or Admin with
`TasksViewAll` sees tasks across their department rather than only their own.
See [Manage tasks](../../staff/tasks/manage-tasks.md).

## See also

- [Add a user](../users/add-a-user.md)
- [Permission matrix](../../reference/permission-matrix.md)
