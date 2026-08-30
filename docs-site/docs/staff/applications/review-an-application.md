# Review an application

**Who can do this:** Admin, Manager, Staff (`ApplicationsView`)
**Where:** Staff Portal → **Academic** → **Applications**
**Before you start:** The student must have submitted an application.

## The list

The staff list is the same table students see, with three differences: the
heading reads **Applications** rather than a welcome message, there is no **New
Application** button, and the one-application-in-progress warning is not shown.

| Column | Shows |
|---|---|
| University | |
| Course | |
| Date Applied | `dd/MM/yyyy` |
| Status | Pending, Approved, Rejected or Studying |
| Actions | |

Search and the status filter work the same way as on the student side — see
[Track application status](../../user/applications/track-application-status.md).

## Open an application

Select **View**. The detail screen shows:

- **Applicant Details** — the student's name, email and application date.
- **Program Selection** — university, course, study mode, campus, description.
- **Address & Emergency Contact** — hometown, current and emergency contact,
  captured against this application rather than the student's account.
- **Required Documents** — the document sections.

The **Enrolment progress** stepper and the **Requested Forms** panel are part of
the student's own view and are not shown to staff here.

Applications that are Approved, Rejected or Studying open read-only, with a
banner naming the status.

::: warning There is no status control on this screen
In the current build the staff application detail page has no button to
move an application between Pending, Approved, Rejected and Studying, and
no reviewer notes field. Reviewing is a read exercise here; progression
happens through the enrolment.

The same limitation that affects students applies to staff — attached
documents are not persisted by the backend, so do not expect to find a
student's uploads on this screen.
:::

## What to do instead

The real workflow runs through enrolments:

1. Read the application here.
2. Create an enrolment for the student —
   [Create an enrolment](../enrolments/create-an-enrolment.md).
3. Work the enrolment's six VISA process steps. Within the **Apply Offer**
   step, each course being pursued is tracked as a **course application** with
   its own status:

| Course application status | Meaning |
|---|---|
| **Init** | Created, not yet submitted to the institution. |
| **Applied** | Submitted to the institution. |
| **Offered** | The institution has made an offer. |
| **Withdrawn** | No longer being pursued. |

Each course application records the university, intake, campus, study mode,
tuition fee, offer applied date, commencement and expected completion dates,
the university offer letter and notes.

This is why a student can see *"3 course options being pursued on your
behalf"* on their own application — those are the course applications on their
enrolment, not separate applications.

## See also

- [Create an enrolment](../enrolments/create-an-enrolment.md)
- [Statuses](../../reference/statuses.md)
- [Track application status](../../user/applications/track-application-status.md) — the student's view
