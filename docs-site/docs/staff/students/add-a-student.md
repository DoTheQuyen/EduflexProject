# Add a student

**Who can do this:** Admin, Manager, Staff (`StudentsAdd`)
**Where:** Staff Portal → **Academic** → **Students**
**Before you start:** Check the student does not already exist — search the list
first. You need their passport details and date of birth.

## The Students list

| Column | Notes |
|---|---|
| Email | Hidden on tablet-width screens and below. |
| First Name | |
| Last Name | |
| Mobile | |
| Passport No. | Hidden on laptop-width screens and below. |
| Active | |

Row buttons depend on your permissions.

## Add a student

1. Select **Add Student**.
2. Complete the form. Every field marked \* is required.
3. Save.

### Account details

| Field | Required |
|---|---|
| **Email** | Yes |
| **Mobile** | Yes |
| **First Name** | Yes |
| **Last Name** | Yes |

### Identity

| Field | Required | Notes |
|---|---|---|
| **Nationality** | Yes | Type-ahead — *Start typing a nationality…* |
| **Passport Number** | Yes | |
| **Date of Birth** | Yes | |

### Address

| Field | Required |
|---|---|
| **Street** | Yes |
| **Suburb** | No |
| **City** | Yes |
| **State** | No |
| **Postal Code** | Yes |
| **Country** | Yes — type-ahead, *Start typing a country…* |

**Result:** The student appears in the **Students** list and can be attached to
applications and enrolments.

::: info Two ways a student record appears
Students also self-register through the public site. A student you create
here and a student who registered themselves end up in the same list — so
always search before adding, or you will create a duplicate that splits
their history across two records.
:::

## Deactivate and reactivate

Open a student's detail page.

| Button | When it appears |
|---|---|
| **Deactivate** | The student is active and your role has `StudentsDelete`. |
| **Reactivate** | The student is inactive and your role has `StudentsEdit`. |

Deactivating is reversible and preserves the student's applications, enrolments
and documents. Prefer it to deletion in every case where the person simply is
not currently active.

::: tip Every list screen works the same way
Search, filters, paging, disappearing columns and conditional row buttons
are covered once in [Working with lists](../../staff/lists.md).
:::

## See also

- [Review an application](../applications/review-an-application.md)
- [Create an enrolment](../enrolments/create-an-enrolment.md)
