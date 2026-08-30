# Manage course promotions

**Who can do this:** Staff with `CoursePromotionsView`, plus Add/Edit/Delete as
needed
**Where:** Staff Portal → **Marketing** → **Course Promotions**

Course promotions are what the **public website** carousel shows. Editing one
changes what prospective students see, so treat this screen as publishing
rather than data entry.

## The list

Promotions are marked **Featured** or **Not featured**, and a **Featured only**
filter narrows the list to the ones currently on show.

## Add or edit a promotion

| Field | Required | Notes |
|---|---|---|
| **University name** | Yes | |
| **Course name** | Yes | Error: *Course name is required*. |
| **Location** | Yes | Error: *Location is required*. |
| **Semester** | Yes | |
| **Tuition** | Yes | |
| **Scholarship** | Yes | For example *20% scholarship*. |
| **Opportunities** | Yes | Error: *Opportunities is required*. |
| **Offer ends** | Yes | Error: *Offer end date is required*. |
| University website URL | No | |
| Display order | No | Controls the position in the carousel. |
| Marketing note | No | Length-limited — *Note is too long* if you exceed it. |

## Featured and display order

Two separate controls decide what appears publicly:

- **Featured** decides *whether* a promotion is in the carousel.
- **Display order** decides *where* in it.

The number of items the carousel shows is capped in
[App settings](../../admin/settings/app-settings.md) under **Course promotions
— default count**. Featuring more promotions than that limit means some never
appear, which looks like a bug and is not one. Check the setting before adding
featured promotions.

## Offer end dates

**Offer ends** is required on every promotion. Expired offers on a public page
cost you enquiries and credibility, so audit the list against today's date
periodically — nothing removes an expired promotion for you.

## Where enquiries land

An enquiry raised from a course promotion carries a link back to it, and the
enquiry detail screen shows a **(view course promotions)** link beside the
subject. That is how you tell which promotion is actually generating interest —
see [Manage enquiries](../enquiries/manage-enquiries.md).

::: tip Every list screen works the same way
Search, filters, paging, disappearing columns and conditional row buttons
are covered once in [Working with lists](../../staff/lists.md).
:::

## See also

- [Manage enquiries](../enquiries/manage-enquiries.md)
- [Student feedback](moderate-feedback.md)
- [App settings](../../admin/settings/app-settings.md)
