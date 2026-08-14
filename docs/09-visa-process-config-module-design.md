# 09 — VISA Process Config Module: Research + Design

**Status:** Design proposal (not yet built). Companion UI mockup published as a Claude artifact in
the same session this doc was written.

**Author's note on scope:** This doc does two things in one pass, because they inform each other:
(A) a grounded research map of what actually happens, end-to-end, when an education agency takes a
student from "wants to study in Australia" through visa grant and into the compliance/post-grant
period — including the parts that go wrong; and (B) a redesign of Eduflex's existing hardcoded
6-step VISA Process tab into a staff-configurable **process template** system, so different steps,
fields, evidence requirements and unlock rules can be designed once (by an admin/senior staff member)
and applied consistently across enrolments — the same template/instance split the
[Dynamic Form module](08-dynamic-form-module-design.md) already established for this codebase.

**Scope update:** Part A originally covered only the student (subclass 500) journey. Part F extends
the same research-then-design treatment to the other Australian visa categories an agency might
plausibly work — Temporary Graduate (485), the skilled-migration family (189/190/491 → 191), Partner,
Parent sponsorship, and Protection — and generalizes the data model in Part C with a `Category`
dimension so a template isn't just "per country" but "per visa category per country." This is what
makes the module a general **VISA module**, not a student-visa-only one, while keeping every category
a business doesn't actually work switched off by default.

---

## Part A — The real-world process (research)

This is the reference journey a genuine Australian international-student case follows. It's broader
than what Eduflex currently models — some phases below (booking/consultation, post-grant compliance,
change-of-provider, non-compliance escalation) have **no home in the app today**. That gap is the
main reason a fixed 6-step enum can't represent the domain, and it directly shapes the step
inventory recommended in Part D.

### Phase 0 — Lead, booking & consultation (pre-enrolment)

This is the only phase Eduflex already has *a* module for — the `Enquiry` collection
(`ShareService/Models/Enquiry/EnquiryModel.cs`) — but it's a raw contact-form record (name, email,
mobile, free-text enquiry, `New/...` status, staff `Response`), not a workflow. There is no modeled
step between "enquiry received" and "enrolment created with a course/intake already chosen." In
practice, agencies run:

- Initial contact triage (channel: walk-in, phone, web enquiry, referral, agent sub-network)
- Course counselling / consultation (often a booked appointment — free or paid depending on the
  business) covering: destination country, course & provider shortlist, indicative CRICOS course
  details, entry requirements (English test scores, prior qualifications), estimated total cost
  (tuition + OSHC + living-cost evidence threshold), and — critically — an early, informal read on
  **Genuine Student (GS)** viability (does the student's study/career narrative and financial
  capacity plausibly support a visa application at all, before money is spent on applications).
- Consultation outcome: proceed to application (→ Phase 1), refer to a different course/country, or
  decline to proceed (documented, not just dropped) — this decision point is where a bad-fit case
  should be filtered out early rather than discovered at COE or visa-lodgement stage.

*Inputs typically captured:* preferred country/course/intake, English test result (if any), highest
qualification + transcript summary, funding source (self/family/sponsor/loan), consultation
date/method, consultant name, outcome + notes.

### Phase 1 — Course application & Letter of Offer

- Course-application(s) submitted to one or more providers (Eduflex already models this as
  `CourseApplicationModel`, a sibling array to the visa steps — `Init → Applied → Offered →
  Finalized/Withdrawn`).
- Provider issues a **Letter of Offer** (conditional or unconditional).
- Student accepts one offer; agency lodges the acceptance + pays any deposit; this is the point
  Eduflex's `FinalizeCourseApplicationAsync` withdraws sibling applications.

*Inputs:* provider, course, intake, campus/study mode, tuition fee quote, offer date, offer
conditions (e.g. subject to English test), deposit amount/date.

### Phase 2 — Enrolment confirmation & service agreement

- Formal enrolment paperwork with the *agency* itself (service agreement, agency fee invoice) runs in
  parallel with the provider offer — this is what Eduflex's existing `EnrolmentForm` step /
  `enrolment-invoice-panel` already covers.
- **Genuine Student (GS) requirement** (replaced the old Genuine Temporary Entrant test on
  23 March 2024): the applicant answers a fixed set of targeted questions *in the visa application
  form itself* (150-word cap per answer, in English) about study intent, course relevance to career
  goals, ties/circumstances, and — new since GS — post-study and permanent-residency intentions are
  **not** held against the applicant the way they were under GTE. Agencies should still capture a
  structured GS worksheet with the student before lodgement, because a weak GS answer is the single
  most common refusal reason.

*Inputs:* signed service agreement, agency fee invoice + payment, GS worksheet answers (draft, for
staff review before the student pastes them into ImmiAccount).

### Phase 3 — CoE (Confirmation of Enrolment)

- Once tuition (or the required deposit) is paid to the *provider*, the provider issues an
  **eCoE** via PRISMS. This is the single hard document gate before a visa application can be
  lodged — Home Affairs will not accept a student visa application without a valid CoE (or evidence
  of intent to obtain one, in the rare in-Australia application case).
- Eduflex already models this well: `CoeCompletion` step requires **both** the `CoE` document *and*
  a `PaymentReceipt` — correctly reflecting that CoE issuance is downstream of payment.

*Inputs:* CoE number, CoE issue date, tuition payment receipt.

### Phase 4 — Visa application lodgement

- Student (or agent on their behalf, within the education-agent/migration-agent boundary — see
  callout below) creates an **ImmiAccount**, completes the online form, pastes in the GS answers,
  uploads supporting documents, and pays the visa application charge (**AUD 1,600** as of 2026).
- Most applicants complete a **health examination** (Bupa Medical Visa Services panel clinic —
  chest X-ray, sometimes full medical) and applicants from some countries attend a **Visa
  Application Centre** for biometrics (fingerprints + photo).
- **OSHC (Overseas Student Health Cover)** must be arranged and evidenced for the full intended
  stay — this satisfies condition 8501 from day one, not just at outcome.
- Financial capacity evidence (2026 threshold: **AUD 29,710** living-cost benchmark, plus
  tuition/travel) must be demonstrable, tied to the funding source captured back in Phase 0.

*Inputs:* ImmiAccount reference, lodgement date, GS answers (final, as submitted), biometrics
booking/date, health-exam booking/date, OSHC policy + expiry, visa application fee receipt.

**Background — two adjacent regulatory regimes:** Education agents sit under **ASQA**/the ESOS
framework; migration advice sits under **OMARA** and the Migration Act 1958 (Migration Agents
Regulations 2026 replaced the 1998 regulations from 1 April 2026). Different bodies, different
practitioner types. C.9's `PractitionerTagModel` catalog and the step-level `PractitionerTagId` exist
as optional staffing/routing metadata a business defines and applies however fits its own
structure — not as an enforced gate.

### Phase 5 — Visa outcome

- **Granted**: visa grant number + conditions letter issued (see Phase 6 for what those conditions
  actually are). Processing benchmark for 2026: ~50% within 29 days, 90% within 56 days for
  higher-ed; VET applications run materially slower (4–7 months); postgraduate research 3–5 months.
- **Refused**: refusal letter states the reason (overwhelmingly the **Genuine Student requirement**
  in current refusal data) and whether review rights exist.
  - **Onshore refusal** → review rights at the **Administrative Review Tribunal (ART)**, generally a
    **21-day statutory deadline** (not extendable) from notification. A major 2026 change (Migration
    Amendment (Administrative Review of Student Visa Refusal Decisions) Regulations 2026, effective
    late May 2026): subclass-500 refusal reviews are now decided **"on the papers"** in most cases —
    no oral hearing — which raises the stakes on getting the *written* review submission complete
    and well-evidenced the first time, since there's often no second chance to explain in person.
  - **Offshore refusal** → generally **no review rights**; the practical path is a fresh application
    addressing the refusal reason, not an appeal.

*Inputs:* outcome, grant number/conditions (if granted) or refusal reason/review-eligibility (if
refused), review-lodgement deadline (if applicable).

### Phase 6 — Post-grant condition monitoring ("changing visa condition in the middle")

A granted subclass 500 visa carries conditions the student must maintain for the life of the visa —
this is the phase Eduflex has **no model for today** (the process currently ends at `VisaOutcome`).
The conditions that actually generate casework:

| Condition | Requirement | Typical trigger for agency involvement |
|---|---|---|
| **8105** | Work no more than 40 hours/fortnight while course is in session (unlimited once course finished/on break) | Student asks whether a job offer breaches hours; needs a plain-English hours calculator/reminder |
| **8202** | Maintain full-time enrolment at the same/higher AQF level, satisfactory attendance & course progress per ESOS Act | **Leading cause of visa cancellation nationwide** — see escalation path below |
| **8501** | Maintain adequate health insurance (OSHC) for the whole stay | Policy renewal reminders; lapses are common and self-inflicted |
| **8517** | Maintain adequate schooling arrangements for school-age dependants in Australia >3 months | Relevant mainly to postgrad/research students with family |
| **8532** | Under-18 students must maintain approved welfare/accommodation arrangements | Age-outs at 18, or accommodation provider changes |

**Mid-course changes that legitimately require action** (this is the other half of "changing visa
condition in the middle" — changes to the *enrolment* that flow through to visa validity):

- **Change of course** (e.g. stepping down an AQF level, or changing provider) can itself put the
  student in breach of 8202 or require a new CoE/visa if it changes course length materially.
- **Change of provider within the first 6 months of the principal course** is restricted by
  **National Code 2018, Part D, Standard 7** — the *new* provider needs a **letter of release** from
  the *current* provider (assessed against that provider's own documented transfer policy; providers
  cannot charge for it; PRISMS itself warns receiving providers if the 6-month rule hasn't been met
  yet). After 6 months, transfer is generally unrestricted. Refusals must be reasoned in writing and
  are appealable through the provider's internal complaints/appeals process.
- **Course completion → Temporary Graduate visa (485)**: since **1 July 2024**, a 485 holder can
  **no longer lodge a subclass 500 application onshore** ("visa hopping" crackdown) — a genuinely new
  student visa from a 485 must be lodged **offshore**. This materially affects "further study"
  advice given to graduating students who are still in Australia on a 485.

### Phase 6b — Non-compliance escalation ("abuse if fail")

This is the concrete, sequenced process behind visa cancellation for course-progress/attendance
failure — the compliance nightmare scenario every agency needs a documented playbook for:

1. Student falls below the provider's course-progress or attendance threshold (commonly ~80%
   attendance, or failing to meet progress benchmarks across **two consecutive study periods**).
2. Provider runs an **intervention strategy** (academic support, counselling, study plan) — this is
   an ESOS Act obligation on the *provider*, not the agency, but the agency is usually the student's
   first call when it happens.
3. If intervention fails, the provider issues a **Notice of Intent to Report** — the student has
   **20 working days** to access the provider's internal complaints-and-appeals process.
4. If the appeal is unsuccessful or not lodged, the provider reports the student via **PRISMS**
   (required within 14 days of the decision to report) — at this point the case leaves the
   provider's control and sits with Home Affairs.
5. Home Affairs may issue a **Notice of Intention to Consider Cancellation (show-cause notice)**
   under **s.116 Migration Act 1958** — this is not a cancellation, it's an invitation to respond,
   typically with a **28-day** response window.
6. If cancellation proceeds: **condition 8202 breach is the single leading cause of student-visa
   cancellation Australia-wide**, and a cancellation on this ground carries a **3-year exclusion
   period** from most further Australian visas. The student retains ART review rights over the
   cancellation decision itself.

*Inputs an agency case-file needs at each hand-off:* intervention date/notes, Notice-of-Intent date,
internal-appeal outcome, PRISMS report date, show-cause notice date + response deadline, response
lodged (Y/N + date), final outcome.

### Phase 7 (optional/future) — Comparable process in other destination countries

Kept deliberately brief — only relevant once/if the agency actively places students into these
markets, per the "optional, expand later" framing of the ask:

- **New Zealand**: apply ≥3 months ahead; INZ's online system typically clears complete files in
  under 3 weeks; recently raised part-time work cap to 25 hrs/week; **Post-Study Work visa** up to
  3 years is the graduate-pathway analogue to Australia's 485.
- **Canada**: 4–12 week processing typical; **Post-Graduation Work Permit (PGWP)** up to 3 years is
  the graduate pathway; provider must be on Canada's Designated Learning Institution list (rough
  analogue to CRICOS registration).
- **UK**: among the fastest major-market processing times; core Student visa route structurally
  unchanged in 2026 but financial-requirement and dependant rules have tightened since 2024 (most
  taught-postgrad students can no longer bring dependants); **Graduate Route** is 2 years post-study.
- **USA**: F-1 process is consulate/interview-driven rather than a single online portal, so it
  doesn't map cleanly onto a CoE-style single-document gate; processing can run several months and is
  consulate-dependent; **OPT** (Optional Practical Training) is the graduate-pathway analogue.

The country differences that actually matter for *this* design are: (a) whether there's a single
"CoE-equivalent" enrolment-confirmation document, (b) whether there's a fixed application-fee/portal
step, (c) what the graduate/post-study pathway is called, and (d) local condition-monitoring
obligations. Part C's data model represents all four as configurable per-template fields specifically
so a "Canada — Standard" or "UK — Standard" template could be authored later without a schema change
— but authoring those templates is out of scope for this pass.

---

## Part B — Current Eduflex architecture (what exists today)

Full detail lives in the codebase; this is the load-bearing summary the redesign in Part C responds
to.

- **`VisaProcessStepKeys`** (`ShareService/Models/Enrolment/VisaProcessStepModel.cs`) is a
  compile-time `string[]` of exactly 6 keys — `StudentInfo, EnrolmentForm, ApplyOffer,
  CoeCompletion, VisaApplication, VisaOutcome` — plus a compile-time `RequiredEvidenceCategory`
  dictionary. Array order *is* the unlock sequence, enforced by index arithmetic in
  `EnrolmentService.CompleteVisaStepAsync`.
- Steps 1–2 (`StudentInfo`, `EnrolmentForm`) store their data as **named, typed properties** directly
  on `EnrolmentModel`; steps 3–6 store data in a **generic `Fields: Dictionary<string,string>`** bag
  on each `VisaProcessStepModel`. Two incompatible storage shapes for "step data" today.
  ApplyOffer/CoeCompletion/VisaOutcome each carry **inline `if (stepKey == VisaProcessStepKeys.X)`
  business rules** in `CompleteVisaStepAsync` (invoice-sent check, finalized-course-application
  check, Granted/Refused validation) — procedural, not data-driven.
- `EnrolmentEnums` (Draft/Offer/Coe/ApplyVisa/VisaSuccess/VisaFail/Cancel/Completed/Finalized) is a
  compiled enum; 4 of its values map 1:1 to 4 step keys via another inline branch, not a lookup
  table.
- Frontend `visa-process-tab.component.html` hand-authors one bespoke template block per step
  (~850 lines) rather than iterating a schema — every field binding is a hardcoded
  `fieldValue('ApplyOffer','offerAppliedDate')`-style string key.
- Permission model is uniform and coarse: every visa-step action (`SaveVisaStepDraftAsync`,
  `CompleteVisaStepAsync`, `ReopenVisaStepAsync`, course-application actions) is gated by a single
  `PermissionKey.EnrolmentsEdit` + strict `OwnerUserId` match, enforced service-side (no
  `[RequirePermission]` on the controller actions themselves) — no per-step granularity.
- No process ends at `VisaOutcome` — nothing in the schema represents Phase 6/6b above.
- The **Dynamic Form module** (docs/08) already solved the adjacent problem of "staff-authorable,
  reusable, versioned content applied per-enrolment" for arbitrary Q&A forms, using a clean
  **template (admin-managed catalog) / instance (per-enrolment, snapshotted at request time)**
  split, one flat admin permission key (`DynamicFormsEdit`), and a `BoundStepKey: string?` that
  already loosely couples a form template to one of today's 6 fixed step keys. This is the pattern
  Part C generalizes.

---

## Part C — Design: the process config module

### C.1 Core principle

Replace the compiled `VisaProcessStepKeys` constant set with a **`VisaProcessTemplateModel`**
(admin-authored, like `DynamicFormTemplateModel`) containing an ordered list of **step
definitions**. Each enrolment snapshots the active template's step definitions into its own
**`VisaProcessSteps`** instance array at creation time — exactly the snapshot-at-request-time
pattern `EnrolmentFormResponseModel.QuestionsSnapshot` already uses, so later template edits never
retroactively change an in-flight enrolment's process. This is the same template/instance
architecture already validated in this codebase for Dynamic Forms — reused deliberately rather than
inventing a second, different pattern (see [[feedback_eduflex_avoid_overengineering]]: prefer the
established shape over a novel one when the problem is structurally the same).

### C.2 Data model

```
VisaProcessTemplateModel : AuditableEntity        // new top-level collection: VisaProcessTemplates
    Id
    Name                        // e.g. "Australia — Standard"
    Country                     // e.g. "AU" — free string, not an enum (keeps NZ/CA/UK/US open)
    Category                    // free string, not an enum — e.g. "Student", "GraduateWork485",
                                 // "SkilledIndependent189", "SkilledNominated190", "SkilledRegional491",
                                 // "PermanentResidenceRegional191", "Partner", "ParentSponsor", "Protection".
                                 // See Part F. A template is really keyed on (Country, Category), not
                                 // Country alone — "one default per Country" below becomes "one default
                                 // per Country+Category" once more than one category exists.
    Description
    Status                      // Active | Inactive  (mirrors DynamicFormTemplateModel.Status)
    IsDefaultForCountry: bool   // one default template selected per Country+Category at enrolment-creation time
    Version: int                // bumped on every published edit; instance snapshots record the version they came from
    Steps: List<VisaProcessStepDefinitionModel>

VisaProcessStepDefinitionModel
    Key                         // free string/slug now, not a compiled const — e.g. "ApplyOffer", "OshcRenewalCheck"
    Order: int
    Label
    Description                 // shown as the step subtitle, replaces STEP_DESCRIPTIONS
    Phase                       // free string grouping for UI sectioning, e.g. "Application", "Compliance" — not enforced, display-only
    Enabled: bool                // the per-business/culture on-off toggle
    CanReopen: bool              // replaces the hardcoded StudentInfo/EnrolmentForm exclusion list
    PractitionerTagId: string?   // optional FK into PractitionerTagModel — see C.9. Null = no tag.
    Fields: List<StepFieldDefinitionModel>   // replaces per-step hardcoded FormGroup controls
    RequiredEvidenceCategories: List<string> // replaces the compiled RequiredEvidenceCategory dictionary; still just category strings
    Preconditions: List<StepPreconditionModel>  // replaces inline if-branches
    SetsEnrolmentStatusTo: EnrolmentEnums?   // replaces the inline enum-branch; null = no status side-effect
    Hints: List<ProcessStepHintModel>        // the senior-experience-sharing feature

PractitionerTagModel : AuditableEntity      // new top-level collection: PractitionerTags — see C.9
    Id
    Name                         // e.g. "Migration-Related", "Senior Caseworker" — business-defined, no fixed vocabulary
    ColorHex
    Description
    Active: bool

StepFieldDefinitionModel
    FieldKey                    // dictionary key inside the instance's Fields bag
    Label
    InputType                   // Text | Date | Number | Select | YesNo  — deliberately small, mirrors FormQuestionModel.AnswerType's restraint
    Options: List<string>       // for Select — replaces the hardcoded Granted/Refused <select>
    IsRequired: bool

StepPreconditionModel            // small, closed vocabulary — NOT a rule engine/DSL (avoid overengineering)
    Type                         // PriorStepFieldNotEmpty | CourseApplicationFinalized | FieldValueIn | AllPriorEvidenceUploaded
    SourceStepKey: string?       // e.g. "EnrolmentForm" for PriorStepFieldNotEmpty
    FieldKey: string?            // e.g. "invoiceId"
    AllowedValues: List<string>? // for FieldValueIn, e.g. ["Granted","Refused"]

ProcessStepHintModel             // append-only, same shape family as AuditTrail/Communications entries elsewhere in this codebase
    Id
    Text                         // rich text — "what I've learned handling this step"
    AuthorUserId / AuthorName
    CreatedAt
    Pinned: bool                 // lets a senior mark their best tip as the one shown collapsed-by-default
```

```
VisaProcessStepModel (instance — evolves the existing model, doesn't replace it)
    Key                          // copied from the definition at snapshot time
    TemplateId / TemplateVersion // which template+version this enrolment's process came from
    Status                       // Locked | Draft | Complete  (unchanged)
    FieldsSnapshot: List<StepFieldDefinitionModel>  // the definition's fields, snapshotted — same reasoning as QuestionsSnapshot
    Fields: Dictionary<string,string>                // answers — unchanged shape, now the *only* storage shape (see C.4)
    CompletedAt / CompletedByUserId / CompletedByName  // unchanged
```

### C.3 Configurability — mapped directly to the ask

- **"Each step will need what info, input etc."** → `Fields: List<StepFieldDefinitionModel>` per
  step definition, rendered by a single generic step-field component instead of hand-authored
  per-step markup. This is also where the closed `Granted/Refused` outcome select becomes
  configurable (`InputType: Select`, `Options: ["Granted","Refused"]`) rather than hardcoded in two
  places (template `.html` and `CompleteVisaStepAsync`'s literal string comparison).
- **"Allow custom input or disable based on different culture of businesses"** → `Enabled: bool` per
  step (a business running referral-only services disables `VisaApplication`/`VisaOutcome`; a
  business that also handles post-grant compliance enables the Phase 6/6b steps proposed in Part D);
  `Fields` list is itself the "custom input" mechanism — a business can add a locale-specific field
  (e.g. a national-ID-style field some source countries need) without a code change. Multiple
  templates per `Country` are supported (only one flagged `IsDefaultForCountry`), so a business
  running visibly different processes for different partner networks isn't forced into one shape.
- **"Allow to add hint or instruction experience sharing from senior"** → `Hints: List<
  ProcessStepHintModel>`, append-only, authored inline from the step's edit panel (both in the
  Process Designer, for pattern-level guidance, and — optionally — directly from the live VISA tab
  by a senior staff member working a real case, the same way `Communications`/`AuditTrail` entries
  get added today). Rendered as a collapsible "Tips from the team" panel on each step, pinned tip
  shown first.
- **Config design module vs. VISA module that applies it** → exactly the Template/Instance split in
  C.2: the Process Designer (admin screens) writes `VisaProcessTemplateModel`; the VISA Process tab
  reads the enrolment's already-snapshotted `VisaProcessSteps` instance array. Editing a template
  never touches any enrolment already in flight — only enrolments created *after* a template edit
  get the new shape (mirrors how Dynamic Form template edits don't retroactively change existing
  `FormResponses`).

### C.4 Reconciling the two storage shapes (StudentInfo/EnrolmentForm vs. the rest)

Today, `StudentInfo`/`EnrolmentForm` are typed `EnrolmentModel` properties while steps 3–6 use the
generic `Fields` dictionary (Part B). Recommendation: **leave `StudentInfo`/`EnrolmentForm` as typed
properties** rather than forcing them into the generic bag. They're structurally different from the
rest of the process — captured once at enrolment creation, not really "steps" a business would ever
disable or reshape — so representing them as configurable step *definitions* (for
consistent numbering/labeling/hints/UI-rendering-as-a-step) while keeping their **data** on the typed
model is the pragmatic middle ground: it satisfies "make steps configurable" without forcing a
storage-format migration onto fields that don't need one. This is a deliberate exception, not an
inconsistency — call it out explicitly in the step definition via a `UsesTypedEnrolmentFields: bool`
flag so the frontend knows to bind to `enrolment.*` instead of `step.fields.*` for those two keys
specifically.

### C.5 Status mapping

`SetsEnrolmentStatusTo: EnrolmentEnums?` on the step definition replaces the inline
key-literal branch in `CompleteVisaStepAsync`. `EnrolmentEnums` itself **stays a compiled enum** —
deliberately not made fully data-driven. The enum represents a small, stable set of pipeline-wide
states other modules key off of (`FinancialRecord` creation gates on `VisaSuccess`/`VisaFail`,
`FinalizeEnrolmentAsync` requires one of those two) — turning it into free-form per-template strings
would ripple into every consumer of `EnrolmentModel.Status` for a benefit (custom status names) the
ask didn't actually request. This is the same avoid-overengineering call as C.2's precondition
vocabulary: make the *mapping* configurable, not the *vocabulary itself*.

### C.6 Permissions

Two-tier split, mirroring Dynamic Forms exactly:

- **New admin-only flat key `VisaProcessTemplatesEdit`** (same shape as `DynamicFormsEdit`/
  `SettingsEdit` — one key, not a View/Add/Edit/Delete set) gates template CRUD, step
  add/remove/reorder/enable-disable, field-schema edits, and precondition/status-mapping edits.
  `GetAllTemplates()`/directory reads stay ungated for any authenticated staff (needed to display
  process phase labels, same precedent as `DynamicFormTemplateService.GetAllAsync()`).
- **Per-enrolment step execution keeps reusing `EnrolmentsEdit` + ownership** — nothing changes here.
  Completing/reopening a step, or adding a hint from the live tab, is still gated by the existing
  `GetOwnedEnrolmentAsync` check. Adding a *hint* specifically should probably be allowed for any
  staff with `EnrolmentsView` (sharing a tip doesn't require edit rights to that specific enrolment)
  — flagged as an open decision in Part F rather than settled here.

### C.7 Frontend

- Replace the ~850-line hand-authored `visa-process-tab.component.html` with a loop over the
  enrolment's `visaProcessSteps` (already ordered), rendering one generic `<app-visa-step-panel>`
  per entry, itself rendering `<app-visa-step-field>` per `FieldsSnapshot` entry (Text/Date/
  Number/Select/YesNo — same small set as Dynamic Form's `AnswerType`, reusing `form-answer-editor`
  where the shapes overlap) plus one `<app-step-evidence-section>` per `RequiredEvidenceCategories`
  entry (already parameterized by category — no change needed there, per survey finding #6) plus a
  `<app-step-hints-panel>`.
- `StudentInfo`/`EnrolmentForm` remain their own slightly bespoke panels per C.4, but still rendered
  inside the same step loop (using their definition's `Label`/`Description`/`Hints` like every other
  step) so the UI doesn't visually distinguish "special" steps from configured ones.
- New admin feature folder `staff-portal/visa-process-templates`, structured like
  `staff-portal/dynamic-forms` (list/new/edit full pages, not popups) — the Process Designer.
- New admin feature folder `staff-portal/practitioner-tags` (list-plus-edit-panel, smaller than the
  Process Designer — see C.9) for the `PractitionerTagModel` catalog; the step-detail editor in the
  Process Designer consumes this catalog as a `<select>` bound to `PractitionerTagId`.
- `VISA_STEP_LABELS`/`VISA_STEP_ORDER`/`VISA_STEP_EVIDENCE_CATEGORY` constants in `models/
  enrolment.ts` (survey finding #9) get replaced by data fetched from the enrolment's own
  `visaProcessSteps` — the Dynamic Form builder's bound-step dropdown (which currently reads
  `VISA_STEP_LABELS`) switches to fetching the *default template's* step list instead, since binding
  a form happens at template-design time, before any specific enrolment exists.

### C.8 Rollout / backward compatibility

1. DB migration seeds exactly one `VisaProcessTemplateModel` — **"Australia — Standard"**,
   `Country: "AU"`, `IsDefaultForCountry: true` — with 6 step definitions that are a byte-for-byte
   match of today's hardcoded `VisaProcessStepKeys` (same keys, same evidence categories, same
   preconditions expressed via the new small vocabulary, same status mapping). No behavior changes
   for existing users on day one.
2. `EnrolmentService`'s `CreateDefault`-equivalent now reads the country's default template and
   snapshots it, instead of calling the old static `VisaProcessStepModel.CreateDefault`.
2b. Existing enrolments' `VisaProcessSteps` arrays (created under the old hardcoded system) need a
   one-time backfill: set `TemplateId`/`TemplateVersion` to the seeded template and synthesize
   `FieldsSnapshot` from that template's current step definitions, since no snapshot exists for them.
3. `CompleteVisaStepAsync`'s inline per-key branches are replaced by the generic precondition/
   status-mapping evaluators, but only after the seeded template is confirmed to express the exact
   same 3 special cases (invoice-sent, course-application-finalized, outcome-selected) through
   `StepPreconditionModel` — this is the acceptance test for the migration, not just "does it
   compile."

### C.9 Practitioner Tags — a small, business-managed catalog

Rather than a fixed `RequiresRegisteredMigrationAgent: bool` baked into the schema, C.2 makes this a
proper catalog (`PractitionerTagModel`, new top-level `PractitionerTags` collection) a business edits
for itself on a dedicated **Practitioner Tags** admin screen — same list-plus-edit-panel shape as the
step detail panel: a left list (swatch, name, description, live count of steps currently using the
tag, Active/Inactive) and a right-hand edit form (name, description, a small preset colour picker,
Active toggle). `VisaProcessStepDefinitionModel.PractitionerTagId` is a plain nullable string FK into
this catalog — no fixed vocabulary, no "Migration" vs "General" binary. A business seeds whatever
labels suit its own structure (the mockup ships three illustrative examples — "Migration-Related",
"Senior Caseworker", "Interpreter Needed" — entirely replaceable). Deactivating a tag doesn't delete
it or clear it off steps already using it (same non-destructive pattern as `Status: Active|Inactive`
elsewhere in this doc) — it just stops appearing as a selectable option for *new* assignments.

**Permission:** reuses `VisaProcessTemplatesEdit` rather than a new key — this catalog is small,
config-adjacent to the process templates themselves, and edited by the same admin audience; a
dedicated key would be exactly the kind of permission sprawl
[[feedback_eduflex_avoid_overengineering]] warns against for a catalog this size (contrast with
`DynamicFormsEdit`, which earned its own key because it gates a materially larger, more
frequently-used surface).

**Not an access-control mechanism:** assigning a tag to a step has no runtime effect anywhere in the
module — no gate on who can complete the step, no enforcement, no warning UI. It's read-only
metadata a business can use however fits its own staffing/routing needs (the template rail also
rolls up which tags appear across a template's steps as small colour dots, purely for at-a-glance
orientation when browsing templates).

---

## Part D — Recommended step inventory for "Australia — Standard (Extended)"

The seeded default template (C.8) matches today's 6 steps exactly, for zero-risk rollout. Separately,
this section proposes an **optional, disabled-by-default** extended set of steps a business can
switch on if it offers services beyond visa-outcome — directly answering Part A's Phase 6/6b gap and
the "changing visa condition in the middle" / "abuse if fail" parts of the ask. None of these are in
the seeded default; a business enables them deliberately via the Process Designer.

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `Consultation` | Pre-enrolment | Record the Phase-0 booking/consultation outcome before an enrolment is even created | consultationDate, consultantName, destinationCountry, courseShortlist, gsPreScreenNotes, outcome (Proceed/Refer/Decline) | none | **Off** — most agencies' consultation happens before an Enrolment record exists at all; enabling this repositions it as the enrolment's first step for agencies that create the record earlier in the funnel |
| `OshcMonitoring` | Compliance | Track OSHC coverage continuity post-grant (condition 8501) | policyProvider, policyNumber, expiryDate, renewalReminderSentAt | `OshcPolicy` | Off |
| `CourseProgressCheck` | Compliance | Periodic 8202 check-in per study period | studyPeriod, attendancePercent, progressStatus (Satisfactory/AtRisk/Unsatisfactory), interventionNotes | none | Off |
| `ChangeOfProvider` | Compliance | Standard 7 release-letter workflow when a student wants to transfer before 6 months | requestedDate, currentProviderDecision, releaseLetterIssued, newCoeNumber | `ReleaseLetter` | Off |
| `NonComplianceCase` | Compliance (escalation) | Structured case file for the intervention → NOICC → PRISMS → show-cause chain from Phase 6b | interventionDate, noticeOfIntentDate, internalAppealOutcome, prismsReportDate, showCauseDate, showCauseResponseDeadline, responseLodged, finalOutcome | `InterventionRecord`, `ShowCauseNotice` | Off |
| `GraduatePathway` | Post-completion | Track transition to 485 or further study | pathwayChosen (485/FurtherStudy/Departing), lodgedOffshore (bool — enforces the post-1-Jul-2024 rule as a hint, not a hard block), newCoeNumber | none | Off |

Each row above is written as if it were already a `VisaProcessStepDefinitionModel` — this table *is*
the seed data for a second, optional template ("Australia — Standard (Extended)") a business can
clone the default from and selectively enable steps out of, rather than authoring from scratch.

---

## Part F — Generalizing to other Australian visa categories

Part A–D treat "VISA module" as synonymous with the student (500) journey, because that's the
category every education-agency enrolment touches. But the ask is broader: 485, 491, 191, 189, 190,
Partner, Parent sponsorship, Protection. These categories don't share a spine the way
Consultation→CoE→Visa Application do for students — a skilled-migration case has no course, no
provider, no CoE at all; a partner case has a *sponsor* as a second party with their own application;
a protection case has no commercial/education angle whatsoever. Trying to force them through the
*same* 6-step shape would be exactly the kind of one-size-fits-all rigidity this redesign exists to
escape. The fix isn't a new mechanism — it's using the mechanism from Part C properly:
**`Category` becomes a second key alongside `Country`** (C.2's data model already reflects this), so
each visa category gets its own from-scratch template with its own step inventory, while still
running through the identical Template→Instance snapshot machinery, the identical `Fields`/
`Preconditions`/`Hints` step shape, and the identical admin permission model.

### F.0 — Category is a staffing/routing dimension, not just a content dimension

Categories differ enough in who typically works them that the per-step `PractitionerTagId` from C.9
is worth applying consistently across Part F's step inventories — purely as a staffing/routing signal
for how a business assigns cases internally, the same spirit as `Phase` grouping steps for display.
It's metadata a business can use however suits its own structure via the Practitioner Tags screen;
the module doesn't attach any behavior to it (no access-control gate, no enforcement).

Practically: a business's `VisaProcessTemplatesEdit`-holding admin enables only the `(Country,
Category)` template combinations it actually works. An education-focused agency might enable
`AU/Student` and `AU/GraduateWork485`; a full-service firm enables all of them. Nothing in the module
forces a category to be used — the default state of every category below other than Student is
**disabled**, same posture as Part D's extended compliance steps.

### F.1 — Temporary Graduate (485)

Directly downstream of the student journey — the natural first category to add after Student, since
many agencies already have the relationship. Two streams as of the 1 March 2026 rule changes:
**Post-Higher Education Work** (2 years for Bachelor's/Master's coursework, 3 years for Research
Master's/PhD; applicant must be under 35) and **Graduate Work** (18 months, tied to an occupation on
the skilled list). Full work rights, no employer/hours restriction, unlike the 500's condition 8105.
Largely a paperwork/eligibility-checking exercise rather than a merits assessment — but note Part A's
Phase 6b finding still applies: a
485 holder **cannot lodge a new 500 onshore** (since 1 Jul 2024), which is exactly why the existing
`GraduatePathway` step (Part D) already carries a hint about this.

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `StreamEligibilityCheck` | Application | Confirm course/age/visa-history eligibility for either stream | qualificationLevel, ageAtLodgement, streamChosen (Post-Higher Ed / Graduate Work) | none | On |
| `EnglishTestEvidence` | Application | IELTS 6.5-equivalent, valid within 1 year of lodgement | testType, score, testDate | `EnglishTestResult` | On |
| `VisaLodgement485` | Application | ImmiAccount lodgement + fee | lodgedDate, applicationId | `VisaPaymentReceipt` | On |
| `Outcome485` | Application | Grant/refuse + validity dates | outcome (Select: Granted/Refused), validFrom, validTo | `VisaGranted` | On |

### F.2 — Skilled migration family: 189 / 190 / 491 → 191

The most structurally different category from Student: there is no course or provider at all — the
spine is a **points-tested Expression of Interest**, not an enrolment. Sequence: (1) skills
assessment from the relevant assessing authority for the nominated occupation, (2) English test,
(3) EOI lodged via **SkillSelect** (not ImmiAccount) carrying the points score, (4) for 190
(state-nominated, permanent) or 491 (state/territory *or* family-sponsored, provisional, regional)
a **nomination** step with the relevant state/territory government — 189 (points-tested, no
sponsorship) skips this; (5) **invitation** issued from the EOI pool, periodic, ranked by points;
(6) full visa application with health/character checks once invited. Minimum 65 points, real
competitive scores much higher; total realistic cost AUD 8,000–15,000+ once assessments/tests/
medicals are included, timeline 6–24 months depending on stream and occupation demand.

**491 → 191** is a second, entirely separate template a business would build once a client's 491 is
granted: it isn't a "next step" of the same application, it's a fresh visa 3+ years later, gated on
condition 8579 (regional residence — home, work, and study all inside a designated regional area for
the whole period) and a **taxable-income threshold** (indexed annually; currently ~AUD 53,900) met in
**3 of the 5 years**, evidenced by ATO Notices of Assessment — business/passive/partner income
doesn't count. This is exactly the kind of multi-year, evidence-drip-fed case a Locked/Draft/Complete
step model with long-lived Draft periods (staff periodically attaching a new NOA as each tax year
closes) fits well.

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `SkillsAssessment` | Eligibility | Positive assessment from the occupation's assessing authority | occupation, assessingAuthority, assessmentOutcome, assessmentDate | `SkillsAssessmentLetter` | On |
| `EnglishTestEvidence` | Eligibility | Points-relevant English test | testType, score | `EnglishTestResult` | On |
| `EoiSubmission` | Application | SkillSelect EOI lodged, points score recorded | pointsScore, visasSelected (189/190/491), eoiDate | none | On |
| `StateNomination` | Application | 190/491 only — state/territory nomination application | state, nominationOutcome, nominationDate | `NominationApproval` | On (skip for 189 — see C.4-style typed-exception note below) |
| `Invitation` | Application | Invitation received from the pool | invitationRound, invitationDate, expiryDate | `InvitationLetter` | On |
| `VisaLodgementSkilled` | Application | Full application + health/character checks | lodgedDate, applicationId | `VisaPaymentReceipt`, `PoliceCheck`, `HealthExam` | On |
| `OutcomeSkilled` | Application | Grant/refuse | outcome (Select: Granted/Refused), grantDate | `VisaGranted` | On |
| `RegionalResidenceTracking` | Compliance (491 only) | Condition 8579 check-ins across the 3–5 year provisional period | checkPeriod, homeAddress, workAddress, studyAddress, compliant (YesNo) | none | On, 491 template only |
| `IncomeThresholdTracking` | Compliance (491 only) | ATO NOA evidence toward the 191 income test | taxYear, taxableIncome, noaReceived (YesNo) | `AtoNoticeOfAssessment` | On, 491 template only |
| `Apply191` | Application (separate template) | The actual 191 lodgement once 3-year/income conditions are met | eligibleFromDate, lodgedDate | `VisaPaymentReceipt` | On, 191 template |

*(`StateNomination`'s "skip for 189" is the same pattern as Part D's `Consultation` step being
off-by-default for businesses that don't need it — 189 and 190/491 are realistically three separate
template clones sharing most steps, not one template with a conditional step, since which steps exist
differs by visa subclass more than by business preference.)*

### F.3 — Partner (820/801 onshore, 309/100 offshore)

Structurally distinct from every other category so far: **two applicants**, the visa applicant and
the **sponsor**, each lodging their own linked ImmiAccount application, and a genuine **two-stage**
process — a temporary visa (820/309) decided first, then automatic reassessment for the permanent
visa (801/100) roughly two years later using largely the same relationship evidence refreshed. Onshore
(820→801) runs 8–15 months to the temporary stage, ~50% of 820s within 16 months, 90% within 24;
offshore (309) 6–12 months. Combined government fee ~AUD 9,365. Relationship evidence is
conventionally organized as **"four pillars"**: financial (joint accounts, shared bills, financial
support), social (joint activities, how the relationship is known to family/friends), household
(shared residence, joint lease/utilities), and commitment (long-term intentions, wills, joint
insurance beneficiaries) — mapping cleanly onto four evidence categories rather than one generic
"relationship evidence" bucket. A hard 2026 operational fact worth encoding as a hint: Home Affairs
now expects applications **decision-ready at lodgement** — a follow-up request for missing documents
is no longer routinely issued, so the evidence-completeness check before lodgement matters more than
it used to. Sponsor eligibility itself is gated (citizen/PR/eligible NZ citizen, character-checked,
capped at 2 sponsorships lifetime / 1 per 5 years) — worth its own step so a disqualifying sponsor
history surfaces before the couple invests in evidence-gathering.

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `SponsorEligibilityCheck` | Eligibility | Citizenship/PR status, character, prior-sponsorship-count check | sponsorStatus, priorSponsorshipCount, eligible (YesNo) | none | On |
| `RelationshipEvidenceFinancial` | Application | Pillar 1 | evidenceSummary | `RelationshipFinancial` | On |
| `RelationshipEvidenceSocial` | Application | Pillar 2 | evidenceSummary | `RelationshipSocial` | On |
| `RelationshipEvidenceHousehold` | Application | Pillar 3 | evidenceSummary | `RelationshipHousehold` | On |
| `RelationshipEvidenceCommitment` | Application | Pillar 4 | evidenceSummary | `RelationshipCommitment` | On |
| `CombinedLodgement` | Application | Linked applicant + sponsor ImmiAccount lodgement | lodgedDate, applicantAppId, sponsorAppId | `VisaPaymentReceipt` | On |
| `StageOneDecision` | Application | 820/309 temporary grant | outcome (Select: Granted/Refused), grantDate | `VisaGranted` | On |
| `StageTwoEligibilityWindow` | Compliance | Tracks the ~2-year wait to the automatic 801/100 reassessment | eligibleFromDate, refreshedEvidenceSubmitted (YesNo) | none | On |
| `StageTwoOutcome` | Application | 801/100 permanent decision | outcome (Select: Granted/Refused), grantDate | `VisaGranted` | On |

### F.4 — Parent sponsorship (103/143/173/864/870)

The category where the *queue*, not the paperwork, dominates the design. Non-contributory (103/804)
currently runs **~30 years**; contributory (143/864) **~12–14 years**, with final processing (as of
28 Feb 2026) only reaching cases queued back to **November 2018** — meaning a template built for this
category needs to represent a step whose realistic "Draft" duration is measured in **years**, not
weeks, and whose main job is queue-position tracking and periodic re-contact rather than active
casework. The **two-stage contributory option** (173/884 temporary first, spreading the ~AUD
48,000 second instalment as 19,420 now + the remainder later) is a genuinely different fee/step
structure from the single-stage 143. The temporary-only **870** (up to 5 years at a time, 10 years
total, no PR pathway) is a different product entirely — not a "stage" of the others, its own
template. Sponsorship itself is lodged and must be **approved before** an 870 visa application (the
inverse order from partner visas, where sponsor and applicant lodge together).

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `SponsorshipApplication` | Application | Child lodges sponsorship (required before 870; parallel for others) | sponsorId, lodgedDate, approved (YesNo) | `SponsorshipApproval` | On |
| `SubclassSelection` | Eligibility | Records which product was chosen and why | subclassChosen (103/143/173/864/870), rationale | none | On |
| `VisaLodgementParent` | Application | Full application + first instalment | lodgedDate, applicationId, firstInstalmentPaid (YesNo) | `VisaPaymentReceipt` | On |
| `QueuePositionTracking` | Compliance | Periodic check against DHA's published processing-queue marker | checkDate, queueMarkerAtCheck, estimatedWait | none | On |
| `SecondInstalmentStage` | Application (contributory only) | The 143/864 second charge, or 173→864/884→? upgrade step | instalmentDue, instalmentPaid (YesNo), paidDate | `VisaPaymentReceipt` | On, contributory templates only |
| `OutcomeParent` | Application | Grant/refuse | outcome (Select: Granted/Refused), grantDate | `VisaGranted` | On |

### F.5 — Protection (866)

The outlier of the whole set, and worth designing deliberately rather than forcing into the same
shape as the rest: there is no sponsor, no course, no employer, no points test — an onshore applicant
presents a **statutory declaration** of persecution-based fear (race/religion/nationality/social
group/political opinion, or complementary-protection grounds), attends an **interview** with a case
officer, and undergoes health/character/security checks before a decision.

| Key | Phase | Purpose | Key fields | Evidence | Default |
|---|---|---|---|---|---|
| `EligibilityScreening` | Eligibility | Initial claim-basis screening | claimBasis (race/religion/nationality/socialGroup/politicalOpinion/complementary), screenedBy | none | On |
| `StatutoryDeclarationDrafting` | Application | Drafting/refining the applicant's statement | draftStatus, lastEditedDate | `StatutoryDeclaration` | On |
| `Lodgement866` | Application | Application + supporting evidence lodged | lodgedDate, applicationId | `VisaPaymentReceipt` | On |
| `InterviewPreparation` | Application | Prep + the interview itself | interviewDate, interviewOutcome notes | none | On |
| `OutcomeProtection` | Application | Grant/refuse; refusal review pathway differs materially (IAA/ART merits review) from the student-visa ART path in Part A | outcome (Select: Granted/Refused), reviewDeadline | `VisaGranted` | On |

### F.6 — What this means for C.2's `IsDefaultForCountry` uniqueness rule

C.2 originally described "one default template per Country." With Category added, the constraint
becomes **one default template per (Country, Category)** pair — e.g. `AU/Student` has its own
default, `AU/SkilledRegional491` has a separate one, and they coexist without conflict. No other
change to the C.2 model is needed; `Category` slots in as a sibling field to `Country` and the
existing snapshot/versioning/permission machinery (C.6–C.8) applies unmodified to every category —
that uniformity is the entire point of building this as one generalized module rather than five
bespoke ones.

---

## Part G — Open decisions for the next pass

1. **Hint authorship permission** — should adding a hint from the live VISA tab require
   `EnrolmentsEdit`, or just `EnrolmentsView` (sharing a tip isn't really "editing" the case)? Leaning
   toward `EnrolmentsView`, not settled.
2. **Where does `Consultation` really belong** — as a step-zero on the Enrolment process template
   (this doc's Part D approach), or as an extension of the existing `Enquiry` module with its own
   booking workflow that *converts into* an Enrolment once a consultation outcome is "Proceed"? The
   latter is arguably more correct (an Enquiry today has no course/intake — it's pre-decision) but is
   a bigger change touching a second module. Flagging rather than deciding here.
3. **Multi-country templates** — this design leaves `Country` as a free string and the NZ/CA/UK/US
   research in Part A deliberately shallow. Don't author those templates until there's a real
   business need — premature templates for markets nobody's placing students into yet is exactly the
   kind of speculative build [[feedback_eduflex_avoid_overengineering]] warns against.
4. **Precondition vocabulary growth** — the 4-type closed vocabulary in C.2 covers every precondition
   found in the current codebase plus the Part D extended steps. If a future step needs a genuinely
   new precondition shape, add a 5th enum value deliberately rather than generalizing into a rule
   engine pre-emptively.
5. **Version pinning UX** — `TemplateVersion` on the instance is captured for audit/debugging, but
   this doc doesn't design a "diff this enrolment's snapshot against the current template" screen.
   Worth doing once templates are actually edited a few times in production and staff start asking
   "why does this old enrolment look different."
6. **Does an Enrolment stay the right container for non-student categories** — Part F's step tables
   assume the existing `EnrolmentModel` (with its `VisaProcessSteps` array) is still where a Skilled/
   Partner/Parent/Protection case lives, since C.2's Category field is scoped to a *template*, not a
   *record type*. That's probably fine for 485 (still tied to a graduating student's existing
   Enrolment) but is a real stretch for Partner/Parent/Protection, which have no course, provider, or
   student concept at all — a differently-named container (e.g. `MigrationCaseModel`) sharing the same
   `VisaProcessSteps`/Template/Instance shape might be the more honest model, at the cost of a second
   top-level collection. Flagging as the single biggest open structural question Part F surfaces, not
   settling it here.
7. **Sponsor/second-party representation** — Partner (F.3) and Parent (F.4) both involve a second
   person (sponsor) with their own eligibility/evidence, which the current single-`Fields`-bag-per-step
   shape doesn't cleanly represent (whose `Fields` does sponsor data belong to?). Likely needs its own
   small `Party` concept (Applicant vs Sponsor) attached to relevant steps rather than forcing sponsor
   data into the applicant's step fields — not designed further here.

---

*Sources for Part A (fetched August 2026): Home Affairs' subclass-500 guidance and Genuine Student
requirement pages; ASQA education-agent and provider-reporting-obligations pages; the Migration
Agents Registration Authority (mara.gov.au) and the Migration Agent Regulations 2026 commencement
coverage; National Code 2018 Standard 7 explanatory guide; multiple 2026 migration-law-firm and
education-consultant summaries of ART review changes, condition 8202 breach/show-cause process, and
the July-2024 485→500 onshore-lodgement restriction. Facts likely to move fastest and worth
re-verifying before this doc is acted on: the AUD figures (application charge, financial-capacity
threshold), and any processing-time benchmarks.*

*Sources for Part F (fetched August 2026): Home Affairs' Temporary Graduate (485), Skilled
Independent/Nominated/Work Regional (189/190/491), Permanent Residence (Skilled Regional) (191),
Partner, Parent, and Protection (866) subclass pages and the March-2026 485 rule-change coverage;
SkillSelect/EOI explainer pages from multiple 2026 migration-law-firm sources; 491→191 income-
threshold and Notice-of-Assessment guidance; partner-visa "four pillars" evidence and April-2026
decision-ready-at-lodgement guidance; parent-visa queue-marker reporting (as at 28 Feb 2026) and the
contributory/non-contributory/two-stage fee-structure breakdown; protection-visa (866) process and
interview-requirement summaries. As with Part A, the dollar figures, queue markers, and age/score
thresholds in Part F are the facts most likely to have moved by the time this doc is acted on —
re-verify against the current Home Affairs pages rather than trusting the numbers here.*
