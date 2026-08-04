# 08 — Dynamic Form Module: Design

Status: **implemented 2026-08-02** — backend + frontend written, grounded in the existing Enrolment
module's conventions ([[project_eduflex_design_docs]] docs 02–04). Not yet built/tested by the user.
See "As-built notes" at the bottom for the handful of places the real implementation deviated from
this doc during the build.

**Decided 2026-08-02:** Forms tab is named "Forms" (§7). Draft saves never write an audit entry —
every other transition does, including Reopen (§6). Reopening a finalized (`Responded`) response
reverses its status back to `Draft` (§2.3, §6). Per-enrolment form actions (request/withdraw/
reopen/staff-edit/export) need no dedicated permission key — access is already governed by
enrolment ownership/assignment, and the student can only ever act on their own form (§5). Template
*management* (the admin catalog) is admin-only behind a single permission key for now, same shape as
`SettingsEdit` — deliberately not split into granular View/Add/Edit/Deactivate keys yet, since it's
a low-frequency, one-time-config screen; revisit only if a real need for finer-grained roles shows up
(§5). **Every finalized response is automatically saved as a PDF into Documents at submit time** —
this isn't an optional staff action, it's part of the Submit flow itself, so a finalized answer is
never only sitting in one place (§2.4, §6, §9).

## 1. What this module does

- Staff define reusable **form templates** (question sets) in an admin screen.
- A template can optionally be **bound to one step** of the Enrolment VISA-process workflow
  (`VisaProcessStepKeys` — `StudentInfo`, `EnrolmentForm`, `ApplyOffer`, `CoeCompletion`,
  `VisaApplication`, `VisaOutcome`). Binding is what makes the "preview + request" affordance show
  up next to that step in the Enrolment detail screen.
- Staff **request** a bound (or unbound) form from the student on a specific Enrolment. This sends
  an email, logs a Communication entry and an Audit entry.
- The student fills the form in **My Applications** (student portal), can save as **Draft**
  repeatedly, then **finalizes** (locks it). Staff can **withdraw** a request or **reopen** a
  finalized response for editing.
- Staff can view/edit the response, preview it as a print page, export it to PDF/Word, and save the
  exported PDF into the Enrolment's Documents list.

## 2. Data model (MongoDB)

Two new pieces, both following the existing Enrolment conventions exactly:

- **`DynamicFormTemplates`** — new **top-level collection**, admin-managed, reusable across many
  enrolments. Same shape as `EmailTemplateModel` (a small reusable catalog collection).
- **`EnrolmentModel.FormResponses`** — new **embedded array field on `EnrolmentModel`**, sibling to
  the existing `Documents`, `Communications`, `AuditTrail`, `VisaProcessSteps` arrays. A form
  response only ever belongs to one enrolment and is always read/written in that context (student
  portal queries "my enrolments" → filters their `FormResponses`, never a cross-enrolment form
  query) — same reasoning the project already used to keep Documents/Communications embedded rather
  than as separate FK'd collections ([[project_eduflex_postgres_migration]] documents this rule for
  the future Postgres schema too: embed 1:1-with-parent data that's never independently joined,
  promote to a real child table only if that changes).

### 2.1 `DynamicFormTemplateModel : AuditableEntity`
*(new file: `ShareService/Models/DynamicForm/DynamicFormTemplateModel.cs`)*

```csharp
public class DynamicFormTemplateModel : AuditableEntity
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    // Active | Inactive — see DynamicFormEnums.TemplateStatus
    [BsonElement("status")]
    public string Status { get; set; } = TemplateStatus.Active.ToString();

    // One of VisaProcessStepKeys.Ordered, or null = not bound to a step
    // (still requestable ad hoc from the Forms tab, just has no step icon).
    [BsonElement("boundStepKey")]
    public string? BoundStepKey { get; set; }

    [BsonElement("questions")]
    public List<FormQuestionModel> Questions { get; set; } = new();
}

public class FormQuestionModel
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("questionText")]
    public string QuestionText { get; set; } = string.Empty;

    [BsonElement("helpText")]
    public string? HelpText { get; set; }

    // RichText | YesNo | SingleSelect | MultiSelect — DynamicFormEnums.AnswerType
    [BsonElement("answerType")]
    public string AnswerType { get; set; } = string.Empty;

    // Only populated for SingleSelect / MultiSelect
    [BsonElement("options")]
    public List<string> Options { get; set; } = new();

    [BsonElement("isRequired")]
    public bool IsRequired { get; set; }
}
```

**Editing a template never touches past responses.** A response snapshots the questions at request
time (below) — this mirrors how `TuitionFee` is prefilled-then-independently-editable, and avoids
silently changing a form a student already answered.

### 2.2 `EnrolmentFormResponseModel` — embedded in `EnrolmentModel.FormResponses`
*(new file: `ShareService/Models/Enrolment/EnrolmentFormResponseModel.cs`, added as a new
`[BsonElement("formResponses")] public List<EnrolmentFormResponseModel> FormResponses` field on
`EnrolmentModel`, alongside `Documents`/`Communications`/`AuditTrail`)*

```csharp
public class EnrolmentFormResponseModel
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("formTemplateId")]
    public string FormTemplateId { get; set; } = string.Empty;

    // Snapshotted at request time so later template edits don't retroactively
    // change a form the student already started or finished.
    [BsonElement("formName")]
    public string FormName { get; set; } = string.Empty;

    [BsonElement("questionsSnapshot")]
    public List<FormQuestionModel> QuestionsSnapshot { get; set; } = new();

    [BsonElement("boundStepKey")]
    public string? BoundStepKey { get; set; }

    // Requesting | Draft | Responded | Withdrawn — DynamicFormEnums.ResponseStatus
    [BsonElement("status")]
    public string Status { get; set; } = ResponseStatus.Requesting.ToString();

    [BsonElement("answers")]
    public List<FormAnswerModel> Answers { get; set; } = new();

    [BsonElement("allowEditAfterFinalize")]
    public bool AllowEditAfterFinalize { get; set; }

    [BsonElement("requestedByUserId")]
    public string RequestedByUserId { get; set; } = string.Empty;
    [BsonElement("requestedByName")]
    public string RequestedByName { get; set; } = string.Empty;
    [BsonElement("requestedAt")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastSavedAt")]
    public DateTime? LastSavedAt { get; set; }
    [BsonElement("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [BsonElement("withdrawnAt")]
    public DateTime? WithdrawnAt { get; set; }
    [BsonElement("withdrawnByUserId")]
    public string? WithdrawnByUserId { get; set; }

    // Set when staff edits the student's answers directly (audit trail already
    // records the action; these two fields make it visible inline on the tab too).
    [BsonElement("staffEditedAt")]
    public DateTime? StaffEditedAt { get; set; }
    [BsonElement("staffEditedByUserId")]
    public string? StaffEditedByUserId { get; set; }

    // Set once staff exports+saves the response as a PDF into Documents —
    // points at the EnrolmentDocumentModel.Id so the Forms tab can link to it.
    [BsonElement("exportedDocumentId")]
    public string? ExportedDocumentId { get; set; }
}

public class FormAnswerModel
{
    [BsonElement("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    // RichText answer text, or "Yes"/"No" for YesNo questions.
    [BsonElement("textValue")]
    public string? TextValue { get; set; }

    // SingleSelect (list of 1) / MultiSelect
    [BsonElement("selectedOptions")]
    public List<string> SelectedOptions { get; set; } = new();
}
```

### 2.3 Enums — `ShareService/Enums/DynamicForms/DynamicFormEnums.cs`

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TemplateStatus { Active = 1, Inactive = 2 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnswerType { RichText = 1, YesNo = 2, SingleSelect = 3, MultiSelect = 4 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResponseStatus { Requesting = 1, Draft = 2, Responded = 3, Withdrawn = 4 }
```

**Reopening a finalized response reuses `Draft`** rather than adding a 5th status — staff "unblock
back to Draft status" is literally the spec's own words. An `EnrolmentFormResponseReopened` audit
entry (§5) carries the "who/when re-opened this" fact, so nothing is lost by not having a separate
status value. This follows [[feedback_eduflex_avoid_overengineering]] — don't add a state the audit
trail already covers.

### 2.4 Ties into existing collections (no schema change needed there)

- **Communication** — every notification-sending status change appends one
  `EnrolmentCommunicationModel` (existing model, unchanged) with `TemplateKey` set to a new
  system `EmailTemplateModel` row, `RecipientType = "Student"`.
- **Audit trail** — every action appends one `EnrolmentAuditEntryModel` (existing model, unchanged).
  New action strings: `FormRequested`, `FormWithdrawn`, `FormSubmitted`, `FormReopenedForEdit`,
  `FormStaffEdited`, `FormExported`.
- **Documents** — saves one `EnrolmentDocumentModel` (existing model, unchanged), `IsFromStudent =
  false`. Shows up in the Documents tab automatically. **This happens automatically as part of
  `SubmitFormAsync`, not as a separate optional step** — the moment a response is finalized, the
  server renders it to PDF and saves it as a Document in the same transaction as the status flip to
  `Responded`, so a finalized answer always exists in two safe places (the response itself + a
  Document snapshot), never only one. `ExportedDocumentId` on the response always points at the
  *latest* generated document. If staff edit the response afterward (`StaffEditFormResponseAsync`) or
  a reopened response gets re-submitted, the PDF is regenerated and re-saved — what happens to the
  previous Document depends on whether the response is bound to a VISA step
  (`EnrolmentFormResponseModel.BoundStepKey`):
  - **Un-bound (ad-hoc) forms** — `Category = "DynamicForm"`, and Documents is treated as an
    append-only list, per the existing `EnrolmentDocumentModel` convention: old snapshots aren't
    deleted, so there's always a record of what the student actually saw and signed at each finalize.
  - **Step-bound forms** (e.g. the GS statement bound to `EnrolmentForm`) — the response is that
    step's one canonical evidence file, same as the step's own manual-upload zone
    (`VISA_STEP_EVIDENCE_CATEGORY`/`EnrolmentService.StepEvidenceCategories` map the step to its
    category — `GS`, `UniOffer`, `CoE`, `VisaDraft`, `VisaGranted`). Each regeneration **deletes the
    previous blob and Documents entry and replaces it**, so the step's evidence list and the
    Documents tab's matching category always show exactly the current version, not a growing pile of
    every past edit.

  The manual "Export PDF / Word" action in the Forms tab is a convenience for staff who want an
  editable Word copy or an on-demand re-render; it doesn't replace the automatic PDF-on-submit save.

## 3. Backend structure (files to add, matching existing module shape 1:1)

| Layer | New file(s) |
|---|---|
| Models | `ShareService/Models/DynamicForm/DynamicFormTemplateModel.cs` (+ `FormQuestionModel`), `ShareService/Models/Enrolment/EnrolmentFormResponseModel.cs` (+ `FormAnswerModel`) |
| Enums | `ShareService/Enums/DynamicForms/DynamicFormEnums.cs` |
| DataAccess | `ShareService/DataAccess/Interface/IDynamicFormTemplate.cs`, `.../Service/DynamicFormTemplate.cs` (Mongo collection accessor — same shape as `IEnrolment`/`Enrolment.cs`) |
| Services | `ShareService/Services/Interface/IDynamicFormTemplateService.cs` + `.../Service/DynamicFormTemplateService.cs` (template CRUD, activate/deactivate). New methods added to `IEnrolmentService`/`EnrolmentService`: `RequestFormAsync`, `WithdrawFormRequestAsync`, `SaveFormDraftAsync`, `SubmitFormAsync` (validates required answers, flips status to `Responded`, renders + saves the PDF into Documents, sets `ExportedDocumentId`, audits, emails — all in one call), `ReopenFormForEditAsync`, `StaffEditFormResponseAsync` (re-renders/re-saves the document too, since the finalized content changed), `ExportFormAsync` (on-demand PDF/Word render for staff, doesn't touch Documents) |
| Documents | New `ShareService/Services/Interface/IFormDocumentRenderer.cs` + `.../Service/FormDocumentRenderer.cs` — renders an `EnrolmentFormResponseModel` + its `QuestionsSnapshot` to a PDF byte stream (print-layout, same visual shape as the builder's live preview panel), called by `SubmitFormAsync`/`StaffEditFormResponseAsync` and by the manual export endpoint |
| Mapping | `ShareService/Mapping/DynamicFormTemplateMappingExtension.cs` (`ApplyEditableFields`, `ToSnapshot()`), `ShareService/Mapping/EnrolmentFormResponseMappingExtension.cs` (construction + `ApplyEditableFields` for the answers-save path) — per [[feedback_eduflex_mapping_centralization]], not inlined in the services |
| Validation | `ShareService/Validations/DynamicForm/DynamicFormTemplateModelValidator.cs`, `.../FormAnswerModelValidator.cs` (required-question enforcement at submit time) |
| DTOs | `Eduflex/DTOs/DynamicForm/*` (template CRUD DTOs; enrolment-side reuses `EnrolmentDto` with the new `formResponses` field — single-model pattern, no separate response DTO per [[feedback_eduflex_single_model_pattern]]) |
| Mapping (web) | `Eduflex/Mapping/DynamicForm/*MappingExtension.cs` (`ToDto()`/`ToModel()`) |
| Controllers | `Eduflex/Controllers/DynamicFormTemplatesController.cs` (admin CRUD); new actions added to the existing `EnrolmentsController` for request/withdraw/draft/submit/reopen/staff-edit/export |
| DB Migration | `030_AddDynamicFormsCollectionAndPermissions_<date>.cs` — creates `DynamicFormTemplates` collection + indexes (`status`, `boundStepKey`), seeds the `DynamicFormRequest` system email template, seeds the single `DynamicFormsEdit` permission row (Admin role only) |

## 4. API surface

**Admin (`DynamicFormTemplatesController`)**
```
GET    /api/DynamicFormTemplates                 list (filter: status, boundStepKey)
GET    /api/DynamicFormTemplates/{id}
POST   /api/DynamicFormTemplates
PUT    /api/DynamicFormTemplates/{id}
POST   /api/DynamicFormTemplates/{id}/deactivate
POST   /api/DynamicFormTemplates/{id}/activate
```

**Staff, on an Enrolment (`EnrolmentsController`)**
```
POST   /api/Enrolments/{id}/forms/request            { formTemplateId }
POST   /api/Enrolments/{id}/forms/{responseId}/withdraw
POST   /api/Enrolments/{id}/forms/{responseId}/reopen
PUT    /api/Enrolments/{id}/forms/{responseId}/answers   staff-edit path — re-renders + re-saves the Document too
GET    /api/Enrolments/{id}/forms/{responseId}/export?format=pdf|docx   on-demand render only, doesn't touch Documents
```

**Student portal, on their own Enrolment (same controller, `[Authorize]` + ownership check —
same pattern already used for other student-facing Enrolment reads)**
```
PUT    /api/Enrolments/{id}/forms/{responseId}/draft     save draft (partial answers OK)
POST   /api/Enrolments/{id}/forms/{responseId}/submit    finalize — validates required Qs, auto-saves
                                                          the finalized PDF into this Enrolment's
                                                          Documents in the same call
```

There is deliberately no separate "save as document" endpoint — that used to be a manual staff
action, but per the 2026-08-02 decision every finalize auto-saves the PDF, so a manual duplicate of
the same save would just create redundant Documents entries.

## 5. Permissions

**Per-enrolment form actions — settled, no new key.** Request/withdraw/reopen/staff-edit/export all
reuse the existing `EnrolmentsEdit` permission: access is already gated by the enrolment itself
(owner/assigned staff), and the student side is gated by ownership (a student only ever sees and
acts on their own Enrolment's `FormResponses`), not by a permission key at all. Adding a key per
button would be exactly the permission sprawl [[feedback_eduflex_avoid_overengineering]] flags for
no real gain here.

**Template management (the admin catalog screen) — settled: admin-only, single key.** Unlike the
per-enrolment actions above, `DynamicFormTemplatesController` (create/edit/deactivate a reusable
template) isn't scoped to any one enrolment, so ownership/assignment doesn't cover it — it needs its
own gate. Confirmed 2026-08-02: this is a one-time-config admin screen ("the details can be changed
over time" but it's not a frequent multi-role workflow), so it gets **one flat permission key** —
`DynamicFormsEdit` — the same shape the app already uses for `SettingsEdit` (§`PermissionKeyEnums`),
rather than a full `View/Add/Edit/Delete` set. Seeded to the Admin role only. If a real need for
finer-grained roles (e.g. a content-editor role that can edit questions but not activate/deactivate)
shows up later, split it then — don't build that now.

## 6. Status/notification rules

| Transition | Trigger | Email? | Audit entry? | Audit action |
|---|---|---|---|---|
| (none) → Requesting | Staff clicks "Request" | ✅ | ✅ | `FormRequested` |
| Requesting/Draft → Draft | Student "Save draft" | ❌ | ❌ | — (temp-saving while the student gathers info; not a meaningful event) |
| Draft → Responded | Student "Finalize" | ✅ (notify staff) | ✅ | `FormSubmitted` — **also auto-saves the finalized PDF into Documents in the same call** (§2.4) |
| Requesting/Draft → Withdrawn | Staff "Withdraw" | ✅ (notify student) | ✅ | `FormWithdrawn` |
| Responded → Draft | Staff "Allow edit" (unblock) | ❌ | ✅ | `FormReopenedForEdit` |
| (answers change, still Responded) | Staff edits response directly | ❌ | ✅ | `FormStaffEdited` — **also re-renders and re-saves the Document** (§2.4) |

Rule: **Draft is the only silent transition** — no email, no audit entry, since it's just the
student's in-progress save while they gather information, not a real event. Every other action
(including Reopen) always writes an audit entry; the three marked ✅ Email also fire the
templated notification. The PDF-into-Documents save isn't its own row here — it's not a status
transition, it's a side effect that rides along inside `FormSubmitted`/`FormStaffEdited`, always in
the same call/transaction so a finalized response and its stored copy can never drift apart.

## 7. Frontend structure

**Admin — new `dynamic-forms` feature folder** (`staff-portal/dynamic-forms`), same shape as
`enrolments`: full pages, no popups, back button — matches the spec and the existing
`enrolment-new`/`enrolment-detail` pattern.
- `dynamic-form-management` — data-table (reuse `<app-data-table>`), Name / Status / Bound step /
  Question count columns, Activate/Deactivate row action.
- `dynamic-form-new` / `dynamic-form-edit` — split layout: left = question builder (add/remove/
  reorder question rows, answer-type selector, options editor for select types, bound-step
  dropdown, status toggle), right = live print-style preview panel.
- Preview panel is a standalone reusable component (`app-form-print-preview`) — reused as-is inside
  the builder, inside the Enrolment step popup, and on the student's read-only "view submitted
  form" screen.

**Staff — Enrolment detail changes**
- `visa-process-tab`: a step with a bound template gets a small form icon; clicking opens
  `<app-modal>` with `app-form-print-preview` + a "Request Student to Answer" button.
- New tab **after Documents, before Communication**, named **"Forms"** — type extends to
  `'visa' | 'documents' | 'forms' | 'communication' | 'audit'`.
  - Sub-tabs, one per distinct form name requested on this enrolment (matches spec: "sub tabs for
    each kind of forms request (form name)").
  - Each sub-tab: status badge, full answers (read view or edit view for staff), a **link to the
    latest auto-saved PDF** (via `ExportedDocumentId`, opens straight from Documents — no separate
    "save as document" button needed since it's already saved), and an action bar — Withdraw, Allow
    student to edit, Edit response (staff), Preview (print), Export PDF/Word (on-demand re-render,
    e.g. to grab an editable Word copy).

**Student portal — Applications module**
- Each application lists its bound Enrolment's requested forms (query: enrolment → `FormResponses`
  where status is not `Withdrawn`).
- Fill page: Save draft / Cancel / Submit. Submit shows a confirm dialog ("you won't be able to
  edit after this — contact staff to unlock") before finalizing.
- After `Responded`, the fill page becomes read-only (still viewable, not editable) until staff
  reopens it (status flips back to `Draft`, at which point editing re-enables automatically).

## 8. As-built notes (where the real implementation deviated from this doc)

Written after finishing the build — these are the concrete places reality diverged from the design
above, kept here rather than silently rewriting history:

1. **PDF rendering reuses `IInvoicePdfService`, not a new `IFormDocumentRenderer`.** While building,
   found the Financial module already has a generic HTML→PDF service
   (`ShareService/Services/Service/Integration/InvoicePdfService.cs`, Playwright headless Chromium)
   used by `FinancialRecordService.GenerateInvoicePdfAsync` — render HTML, upload to blob, store the
   URL. Dynamic Forms reuses this directly (`EnrolmentService` now takes `IInvoicePdfService` as a
   dependency) instead of inventing a parallel PDF interface — same "strict current system structure"
   reasoning the build was asked to follow. `EnrolmentFormResponseMappingExtension.RenderToHtml()`
   (ShareService/Mapping) builds the print-style HTML string; `SaveResponseAsDocumentAsync` in
   `EnrolmentService` does the render+upload+Documents-append. §8's "PDF library" open item is
   resolved by this — no QuestPDF, no new interface.
2. **`ExportFormAsync` returns a blob URL (`string`), not raw `byte[]`.** Matches
   `GenerateInvoicePdfAsync`'s established pattern (nothing in this codebase streams PDF bytes
   through a controller action) — renders, uploads to blob storage, returns the URL. Frontend just
   does `window.open(url)`.
3. **`AllowEditAfterFinalize` field dropped from `EnrolmentFormResponseModel`.** It was redundant
   with the status itself — `ReopenFormForEditAsync` already flips `Responded` → `Draft`, and *that*
   status is the editability signal (student-side ownership checks gate on status, not a separate
   flag). One less field to keep in sync.
4. **No separate `FormAnswerModelValidator`.** Required-question enforcement at submit time is a
   plain inline check in `EnrolmentService.SubmitFormAsync` (`ValidateRequiredAnswers`), mirroring
   how `CompleteVisaStepAsync` already enforces its own business rules (required evidence category)
   inline rather than via FluentValidation — cross-collection validation (comparing
   `QuestionsSnapshot` against submitted `Answers`) doesn't fit FluentValidation's per-property model
   well, and this codebase already has the "inline business rule in the service" precedent.
5. **`GET by-application/{applicationId}/forms` returns `MyEnrolmentFormsDto { EnrolmentId, Forms }`,
   not a bare list.** The student-facing Applications screen only ever knows its own `applicationId`
   — it needs the linked Enrolment's id back to call the draft/submit endpoints, which are scoped by
   enrolment id. Caught this gap partway through the frontend build (the original plan would have
   left the student with no way to call `PUT .../draft`) and fixed the DTO/controller/frontend
   together rather than leaving it broken.
6. **Template admin `GetAll()` is a genuinely open directory, not gated behind `DynamicFormsEdit` at
   all.** Necessary once the VISA-step popup and the Enrolment Forms tab needed to list *requestable*
   templates for ordinary Staff/Manager users, not just Admins — `DynamicFormsEdit` only gates
   create/update/activate/deactivate (the admin catalog screen), matching
   `DepartmentsController.GetDepartmentsDirectory`'s existing "lightweight, non-permission-gated
   directory for pickers" precedent. This is a necessary consequence of §5's decision, not a
   contradiction of it — §5 only ever covered *managing* the catalog, not reading it.
7. **`VISA_STEP_LABELS` promoted from a local const in `visa-process-tab.component.ts` into
   `models/enrolment.ts`.** The Dynamic Forms admin builder's bound-step dropdown needed the same
   step-name labels the VISA Process tab already had hardcoded locally — moved to the shared model
   file and `visa-process-tab.component.ts` now imports it too, so there's one source of truth
   instead of two copies drifting apart.
8. **New shared CSS**: `.badge-pill-muted-soft` added to `theme.css` (Inactive/Withdrawn tone — no
   existing neutral/muted badge-pill variant existed) and a new `app-form-print-preview` +
   `app-form-answer-editor` pair of generic components (`src/generic-components/`), reused across the
   builder's live preview, the VISA-step popup, the staff Forms tab, and the student fill/view screens
   — written once rather than four times, per this project's own component-reuse conventions.

**Still open**: Word export (`.docx`) was intentionally not implemented — the on-demand Export action
only produces PDF for now. Add a second `IWordDocumentRenderer`-style implementation later if needed;
nothing else in the design depends on it.

## 9. Amendments after first hands-on test (2026-08-02, later same day)

Testing the built module surfaced gaps the original design missed. Fixed:

- **A form can only be requested once per enrolment at a time.** `RequestFormAsync` now rejects a
  request if any non-`Archived` response already exists for that template on this enrolment. This
  wasn't in the original design — nothing prevented staff from requesting the same form repeatedly,
  which produced confusing duplicate sub-tabs in testing.
- **New `Archived` status** (`ResponseStatus.Archived = 5`) — the release valve for the rule above.
  Only a `Withdrawn` response can be archived (`ArchiveFormResponseAsync`, new endpoint
  `POST .../forms/{responseId}/archive`); archiving is what frees the template up for a fresh
  request. Archived responses stay visible in the Forms tab as history, just muted-badged.
- **Reopening now goes to `Requesting`, not `Draft`, and re-sends the request email.** This
  **reverses** the original 2026-08-02 decision ("Reopening ❌ no email") — reasoning at the time was
  that Reopen is a quiet, low-ceremony action. In practice, a silent status flip gives the student no
  way to know their form needs attention again, so `ReopenFormForEditAsync` now behaves like a second
  `RequestFormAsync`: status → `Requesting`, `dynamic-form-request` email fires again. The rest of
  §6's rule ("only transitions *into* Draft are silent") still holds — Reopen was never actually a
  transition into Draft in the first place once this changed.
- **PDF filename convention**: `{FormName}-{StudentName}-{yyyyMMdd-HHmmss}.pdf`, applied consistently
  to both the auto-save-on-submit path and the on-demand Export.
- **PDF/print-preview header**: dropped the "Eduflex" org label and the raw Enrolment `Id` (neither
  meant anything to a human reading the document) in favour of the student's email and phone —
  applies to both the generated PDF (`RenderToHtml`) and the live `app-form-print-preview` component
  (`reference` input replaced by `studentEmail`/`studentPhone`).
- **Question hint text**: `FormQuestionModel.HelpText` already existed in the model and rendered in
  both the print-preview and the PDF, but the admin builder never exposed a field to actually type it
  — added the missing input in `dynamic-form-edit.component.html`.
