# Manage enquiries

**Who can do this:** Staff with `EnquiryView`; responding also needs `EnquiryEdit`
**Where:** Staff Portal → **Enquiry**
**Before you start:** Nothing.

An enquiry is an initial contact from a prospective student, before any
application exists.

## The list

Four tabs, one per status. An enquiry appears in exactly one of them.

| Tab | Contains |
|---|---|
| **New** | Received, not yet actioned. |
| **More Info Requested** | You have asked the enquirer for more detail. |
| **Responded** | A reply has been sent. |
| **Converted** | The enquiry became a student or an application. |

The columns change between tabs.

**New tab**

| Column | Notes |
|---|---|
| Name | First and last name combined. |
| Email | Hidden on laptop-width screens and below. |
| Mobile | Hidden on laptop-width screens and below. |
| Subject | |
| Received | When it arrived. |

**Responded, More Info Requested and Converted tabs**

| Column | Notes |
|---|---|
| Name | |
| Subject | |
| Responded By | The staff member who actioned it. |
| Responded At | When. |

Search with *Search name or email…*. The list is paged server-side. An empty tab
shows *No enquiries in this tab.*

| Row button | When it appears |
|---|---|
| **View** | Always. |
| **Delete** | Only on the **New** tab, and only if your role has `EnquiryDelete`. |

::: info Delete is deliberately limited
Once an enquiry has been responded to there is a record of that contact, so
it can no longer be deleted from the list. Spam and duplicates can be
cleared from **New** before anyone replies.
:::

## Respond to an enquiry

1. Select **View** on the enquiry.
2. The detail screen shows two panels: **Respond** on the left, **Customer
   Details** on the right.
3. In **Respond**, choose the new **Status**.
4. Write the **Response message**.
5. Select the submit button.

The submit button is disabled if your role lacks `EnquiryEdit` — you can read
enquiries with view permission alone, but not action them.

**Result:** The enquiry moves to the tab matching its new status, and your name
and the time are recorded as *Responded By* and *Responded At*.

## Customer Details

Read-only, showing everything the enquirer submitted:

| Field | Notes |
|---|---|
| Name | First, middle and last. |
| Email | |
| Mobile | |
| Subject | If the enquiry came from a course promotion, a **(view course promotions)** link appears beside it. |
| Received | |
| Message | The full enquiry text, with the enquirer's original line breaks preserved. |

## Working the queue

Use the tabs as a workflow rather than a filter:

1. Work **New** down to empty.
2. **More Info Requested** is your follow-up list — nothing progresses until the
   enquirer replies.
3. **Responded** is the pool to convert.
4. **Converted** is history.

::: tip Every list screen works the same way
Search, filters, paging, disappearing columns and conditional row buttons
are covered once in [Working with lists](../../staff/lists.md).
:::

## See also

- [Add a student](../students/add-a-student.md) — the usual next step
- [Statuses](../../reference/statuses.md)
