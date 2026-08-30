# Manage practitioner tags

**Who can do this:** Admin (`VisaProcessTemplatesEdit`)
**Where:** Staff Portal → **Template** → **Practitioner Tags**

Practitioner tags are a small catalog of labels applied to steps in a
[visa process template](configure-a-visa-process-template.md). They mark which
kind of practitioner a step belongs to.

::: info Shared permission
This screen uses the same key as VISA Process Templates. There is no
separate practitioner-tag permission — the catalog is small and
config-adjacent to the templates that consume it.
:::

## The list

Filter with **All tags** or **Inactive** to see retired ones.

## Add or edit a tag

| Field | Notes |
|---|---|
| **Name** | For example *Senior Caseworker*. |
| **Description** | *What this label means for staff* — a plain-English explanation. |
| **Colour** | How the tag renders wherever it appears. |

## Write the description properly

The description is the only place the tag's meaning is recorded. A tag called
*Senior Caseworker* with no description leaves whoever configures the next
template guessing whether it means seniority, a licence, or a team.

Choose distinguishable colours too — tags are read at a glance on a step, and
three similar blues defeat the purpose.

## Retiring a tag

Set a tag **Inactive** rather than deleting it. Templates and cases that already
reference it keep working, and it stops being offered on new steps.

## See also

- [Configure a visa process template](configure-a-visa-process-template.md)
- [Permission matrix](../../reference/permission-matrix.md)
