import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Client, EducationPartnerDto, SetMaxApplicationsOverrideDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { SettingsService } from '@services/settings.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { ModalComponent } from '@generic/modal/modal.component';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { DynamicFormTemplate, EnrolmentFormResponse } from '@app/models/dynamic-form';
import {
  Enrolment, EnrolmentDocument,
  VisaStepKey, VisaProcessStep, VISA_STEP_ORDER, VISA_STEP_EVIDENCE_CATEGORY, VISA_STEP_LABELS
} from '../../../../../../../models/enrolment';
import { StepEvidenceSectionComponent } from './step-evidence-section/step-evidence-section.component';
import { CourseApplicationsPanelComponent } from './course-applications-panel/course-applications-panel.component';
import { EnrolmentInvoicePanelComponent } from './enrolment-invoice-panel/enrolment-invoice-panel.component';

const STEP_DESCRIPTIONS: Record<VisaStepKey, string> = {
  StudentInfo: 'Personal details captured at enquiry conversion',
  EnrolmentForm: 'Course, intake & Genuine Student statement',
  ApplyOffer: 'Submit application, upload the university offer',
  CoeCompletion: 'Confirmation of Enrolment from the institution',
  VisaApplication: 'Lodge the student visa application',
  VisaOutcome: 'Final step — grant decision & study start'
};

@Component({
  selector: 'app-visa-process-tab',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule, ModalComponent, FormPrintPreviewComponent,
    StepEvidenceSectionComponent, CourseApplicationsPanelComponent, EnrolmentInvoicePanelComponent
  ],
  templateUrl: './visa-process-tab.component.html',
  styleUrls: ['./visa-process-tab.component.css']
})
export class VisaProcessTabComponent implements OnInit, OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() partners: EducationPartnerDto[] = [];
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();
  // Staff-facing "Update" link on a completed-online form (e.g. GS statement) routes to
  // the Forms tab instead of duplicating its edit UI here — see gsFormResponse.
  @Output() goToFormsTab = new EventEmitter<void>();

  readonly stepOrder = VISA_STEP_ORDER;
  readonly stepLabels = VISA_STEP_LABELS;
  readonly stepDescriptions = STEP_DESCRIPTIONS;
  readonly stepEvidenceCategory = VISA_STEP_EVIDENCE_CATEGORY;

  editingStudent = false;
  editingEnrolment = false;
  studentForm: FormGroup;
  enrolmentForm: FormGroup;
  isSaving = false;

  // VISA Outcome is the one step that can still be corrected after being marked
  // Complete (the backend allows re-completing it specifically) — this toggle re-opens
  // its fields for editing, mirroring the editingStudent/editingEnrolment pattern above.
  editingVisaOutcome = false;

  openStepKey: VisaStepKey | null = null;
  stepFieldsDraft: Partial<Record<VisaStepKey, Record<string, string>>> = {};
  isSavingStep = false;
  isCompletingStep = false;
  private hasInitializedOpenStep = false;
  // ----- Dynamic Forms — bound-step marker + preview/request popup -----
  activeTemplates: DynamicFormTemplate[] = [];
  formPopupStepKey: VisaStepKey | null = null;
  isRequestingBoundForm = false;

  // ----- Enrolment Form step — "Number applications allowed" header control -----
  globalMaxApplications = 1;
  maxApplicationsInput = 1;
  isSavingMaxApplications = false;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private dynamicFormTemplateService: DynamicFormTemplateService,
    private settingsService: SettingsService,
    private notificationService: NotificationService
  ) {
    this.studentForm = this.fb.group({
      firstName: [''], middleName: [''], lastName: [''],
      dateOfBirth: [''], gender: [''], email: [''], mobile: [''],
      nationality: [''], passportNumber: [''],
      hometownStreet: [''], hometownSuburb: [''], hometownCity: [''], hometownState: [''], hometownPostcode: [''], hometownCountry: [''],
      currentStreet: [''], currentSuburb: [''], currentCity: [''], currentState: [''], currentPostcode: [''], currentCountry: [''],
      emergencyName: [''], emergencyRelationship: [''], emergencyPhone: [''], emergencyEmail: ['']
    });

    this.enrolmentForm = this.fb.group({
      fundingSource: [''], visaStatus: [''], status: ['']
    });
  }

  ngOnInit(): void {
    this.dynamicFormTemplateService.getAll().subscribe({
      next: (templates) => { this.activeTemplates = templates.filter(t => t.status === 'Active'); },
      error: () => {}
    });

    this.settingsService.getSettings().subscribe({
      next: (settings) => {
        this.globalMaxApplications = settings.maxApplicationsPerStudent ?? 1;
        this.maxApplicationsInput = this.enrolment.maxApplicationsOverride ?? this.globalMaxApplications;
      },
      error: () => {}
    });
  }

  // ----- Enrolment Form step — "Number applications allowed" header control -----

  get effectiveMaxApplications(): number {
    const override = this.enrolment.maxApplicationsOverride;
    return override != null ? Math.min(override, this.globalMaxApplications) : this.globalMaxApplications;
  }

  get hasFinalizedCourseApplication(): boolean {
    return this.enrolment.courseApplications.some(c => c.status === 'Finalized');
  }

  saveMaxApplications(): void {
    if (!this.canEdit()) return;
    if (this.maxApplicationsInput < 1 || this.maxApplicationsInput > this.globalMaxApplications) {
      this.notificationService.error(`Number applications allowed must be between 1 and the system maximum (${this.globalMaxApplications}).`);
      return;
    }

    this.isSavingMaxApplications = true;
    this.apiClient.maxApplications(this.enrolment.id, new SetMaxApplicationsOverrideDto({ maxApplications: this.maxApplicationsInput })).subscribe({
      next: () => {
        this.isSavingMaxApplications = false;
        this.notificationService.success('Number applications allowed updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingMaxApplications = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not update the applications limit.'));
      }
    });
  }

  // ----- Dynamic Forms — bound-step marker + preview/request popup -----

  boundTemplateFor(stepKey: VisaStepKey): DynamicFormTemplate | undefined {
    return this.activeTemplates.find(t => t.boundStepKey === stepKey);
  }

  isAlreadyRequested(templateId: string): boolean {
    return this.enrolment.formResponses.some(r => r.formTemplateId === templateId && r.status !== 'Withdrawn');
  }

  openFormPopup(stepKey: VisaStepKey): void {
    this.formPopupStepKey = stepKey;
  }

  closeFormPopup(): void {
    this.formPopupStepKey = null;
  }

  get formPopupTemplate(): DynamicFormTemplate | undefined {
    return this.formPopupStepKey ? this.boundTemplateFor(this.formPopupStepKey) : undefined;
  }

  // Most recent live (non-Withdrawn/Archived) response for the popped-up step's bound
  // template — lets the popup show what the student actually answered instead of
  // always falling back to the blank template.
  get formPopupResponse(): EnrolmentFormResponse | undefined {
    return this.latestResponseFor(this.formPopupTemplate?.id);
  }

  get formPopupHasAnswers(): boolean {
    return (this.formPopupResponse?.answers.length ?? 0) > 0;
  }

  private latestResponseFor(templateId: string | undefined): EnrolmentFormResponse | undefined {
    if (!templateId) return undefined;
    return [...this.enrolment.formResponses]
      .filter(r => r.formTemplateId === templateId && r.status !== 'Withdrawn' && r.status !== 'Archived')
      .sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime())[0];
  }

  // GS statement's bound response — drives the "completed online, no manual upload"
  // gate in the Enrolment Form step's GS section, see docs/08 §2.4.
  get gsFormResponse(): EnrolmentFormResponse | undefined {
    return this.latestResponseFor(this.boundTemplateFor('EnrolmentForm')?.id);
  }

  get gsCompletedOnline(): boolean {
    return this.gsFormResponse?.status === 'Responded';
  }

  openGsResponsePreview(): void {
    this.openFormPopup('EnrolmentForm');
  }

  requestBoundForm(): void {
    const template = this.formPopupTemplate;
    if (!template || !this.canEdit()) return;

    this.isRequestingBoundForm = true;
    this.enrolmentService.requestForm(this.enrolment.id, template.id).subscribe({
      next: () => {
        this.isRequestingBoundForm = false;
        this.notificationService.success('Form requested — an email has been sent to the student.');
        this.closeFormPopup();
        this.changed.emit();
      },
      error: (err) => {
        this.isRequestingBoundForm = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not request this form.'));
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['enrolment'] || !this.enrolment) return;

    this.patchForms(this.enrolment);
    this.initStepFieldsDraft(this.enrolment);

    if (!this.hasInitializedOpenStep) {
      this.openStepKey = this.enrolment.visaProcessSteps.find(s => s.status === 'Draft')?.key ?? null;
      this.hasInitializedOpenStep = true;
    }

  }

  private patchForms(e: Enrolment): void {
    this.studentForm.reset({
      firstName: e.firstName, middleName: e.middleName ?? '', lastName: e.lastName,
      dateOfBirth: e.dateOfBirth ? e.dateOfBirth.substring(0, 10) : '',
      gender: e.gender ?? '', email: e.email, mobile: e.mobile,
      nationality: e.nationality ?? '', passportNumber: e.passportNumber ?? '',
      hometownStreet: e.hometownAddress?.street ?? '', hometownSuburb: e.hometownAddress?.suburb ?? '',
      hometownCity: e.hometownAddress?.city ?? '', hometownState: e.hometownAddress?.state ?? '',
      hometownPostcode: e.hometownAddress?.postalCode ?? '', hometownCountry: e.hometownAddress?.country ?? '',
      currentStreet: e.currentAddress?.street ?? '', currentSuburb: e.currentAddress?.suburb ?? '',
      currentCity: e.currentAddress?.city ?? '', currentState: e.currentAddress?.state ?? '',
      currentPostcode: e.currentAddress?.postalCode ?? '', currentCountry: e.currentAddress?.country ?? '',
      emergencyName: e.emergencyContact?.name ?? '', emergencyRelationship: e.emergencyContact?.relationship ?? '',
      emergencyPhone: e.emergencyContact?.phone ?? '', emergencyEmail: e.emergencyContact?.email ?? ''
    });

    this.enrolmentForm.reset({
      fundingSource: e.fundingSource ?? '', visaStatus: e.visaStatus ?? '', status: e.status
    });
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  toggleEditStudent(): void {
    if (!this.canEdit()) return;
    if (this.editingStudent) { this.patchForms(this.enrolment); }
    this.editingStudent = !this.editingStudent;
    this.openStepKey = 'StudentInfo';
  }

  toggleEditEnrolment(): void {
    if (!this.canEdit()) return;
    if (this.editingEnrolment) { this.patchForms(this.enrolment); }
    this.editingEnrolment = !this.editingEnrolment;
    this.openStepKey = 'EnrolmentForm';
  }

  private canEdit(): boolean {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit enrolments.');
      return false;
    }
    if (!this.isOwner) {
      this.notificationService.error('Only the staff member who owns this enrolment can edit it. Ask a manager to reassign it to you.');
      return false;
    }
    return true;
  }

  save(section: 'student' | 'enrolment'): void {
    if (!this.canEdit()) return;

    const s = this.studentForm.value;
    const en = this.enrolmentForm.value;

    const payload: Enrolment = {
      ...this.enrolment,
      firstName: s.firstName, middleName: s.middleName || undefined, lastName: s.lastName,
      dateOfBirth: s.dateOfBirth || undefined, gender: s.gender || undefined,
      email: s.email, mobile: s.mobile, nationality: s.nationality || undefined, passportNumber: s.passportNumber || undefined,
      hometownAddress: { street: s.hometownStreet, suburb: s.hometownSuburb, city: s.hometownCity, state: s.hometownState, postalCode: s.hometownPostcode, country: s.hometownCountry },
      currentAddress: { street: s.currentStreet, suburb: s.currentSuburb, city: s.currentCity, state: s.currentState, postalCode: s.currentPostcode, country: s.currentCountry },
      emergencyContact: { name: s.emergencyName, relationship: s.emergencyRelationship, phone: s.emergencyPhone, email: s.emergencyEmail },
      // Course/intake/study mode/campus/dates/tuition/notes now live per-course-application
      // (see CourseApplication) rather than once on the enrolment itself — nothing here
      // overrides them, so update() leaves whatever's already stored untouched.
      fundingSource: en.fundingSource || undefined, visaStatus: en.visaStatus || undefined,
      status: en.status
    };

    this.isSaving = true;
    this.enrolmentService.update(this.enrolment.id, payload).subscribe({
      next: () => {
        // update() only touches the enrolment's own fields (name/address/etc.), not
        // visaProcessSteps — the staff comment box on these two steps lives in the
        // fields bag, so it needs its own save call alongside.
        const stepKey: VisaStepKey = section === 'student' ? 'StudentInfo' : 'EnrolmentForm';
        this.enrolmentService.saveVisaStepDraft(this.enrolment.id, stepKey, this.stepFieldsDraft[stepKey] ?? {}).subscribe();

        this.isSaving = false;
        this.editingStudent = false;
        this.editingEnrolment = false;
        this.notificationService.success('Enrolment updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSaving = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save changes. Please try again.'));
      }
    });
  }

  getStep(key: VisaStepKey): VisaProcessStep | undefined {
    return this.enrolment.visaProcessSteps.find(s => s.key === key);
  }

  private initStepFieldsDraft(enrolment: Enrolment): void {
    this.stepFieldsDraft = {};
    enrolment.visaProcessSteps.forEach(s => {
      this.stepFieldsDraft[s.key] = { ...s.fields };
    });
  }

  fieldValue(key: VisaStepKey, field: string): string {
    return this.stepFieldsDraft[key]?.[field] ?? '';
  }

  setFieldValue(key: VisaStepKey, field: string, value: string): void {
    if (!this.stepFieldsDraft[key]) { this.stepFieldsDraft[key] = {}; }
    this.stepFieldsDraft[key]![field] = value;
  }

  toggleStepPanel(key: VisaStepKey): void {
    const step = this.getStep(key);
    if (!step || step.status === 'Locked') return;
    this.openStepKey = this.openStepKey === key ? null : key;
  }

  stepEvidenceDocuments(key: VisaStepKey): EnrolmentDocument[] {
    const category = this.stepEvidenceCategory[key];
    if (!category) return [];
    return this.enrolment.documents.filter(d => d.category === category);
  }

  // Client-computed checklist for the warning-zone sidebar — no new backend calls, just
  // scans the same visaProcessSteps/documents data already loaded for the accordion.
  get outstandingItems(): string[] {
    const items: string[] = [];

    for (const key of this.stepOrder) {
      const step = this.getStep(key);
      if (!step || step.status === 'Locked' || step.status === 'Complete') continue;

      const category = this.stepEvidenceCategory[key];
      if (category && this.stepEvidenceDocuments(key).length === 0) {
        items.push(`${this.stepLabels[key]}: ${category} evidence not uploaded`);
      }
    }

    const invoiceId = this.fieldValue('EnrolmentForm', 'invoiceId');
    if (!invoiceId) {
      items.push('Enrolment service-fee invoice has not been sent');
    } else if (!this.fieldValue('EnrolmentForm', 'invoicePaidAt')) {
      items.push('Enrolment service-fee invoice sent but not yet marked paid');
    }

    const coeStep = this.getStep('CoeCompletion');
    if (coeStep && coeStep.status !== 'Locked' && coeStep.status !== 'Complete'
      && !this.enrolment.courseApplications.some(c => c.status === 'Finalized')) {
      items.push('No course application has been finalized yet');
    }

    return items;
  }

  canCompleteStep(key: VisaStepKey): boolean {
    const category = this.stepEvidenceCategory[key];
    const hasEvidence = !category || this.stepEvidenceDocuments(key).length > 0;
    if (key === 'VisaOutcome') {
      const outcome = this.fieldValue('VisaOutcome', 'outcome');
      return hasEvidence && (outcome === 'Granted' || outcome === 'Refused');
    }
    // Enrolment Form auto-completes at enrolment creation, so there's no "complete this
    // step" action to gate on the service-fee invoice — the gate lives on Apply Offer
    // instead (mirrored server-side in EnrolmentService.CompleteVisaStepAsync, so this
    // is UX only, not the real enforcement).
    if (key === 'ApplyOffer' && !this.fieldValue('EnrolmentForm', 'invoiceId')) {
      return false;
    }
    // Mirrors the server-side check in EnrolmentService.CompleteVisaStepAsync — a course
    // application must have gone through Finalize before CoE Completion can be confirmed.
    if (key === 'CoeCompletion' && !this.enrolment.courseApplications.some(c => c.status === 'Finalized')) {
      return false;
    }
    return hasEvidence;
  }

  toggleEditVisaOutcome(): void {
    if (!this.canEdit()) return;
    this.editingVisaOutcome = !this.editingVisaOutcome;
  }

  saveStepDraft(key: VisaStepKey): void {
    if (!this.canEdit()) return;
    this.isSavingStep = true;
    this.enrolmentService.saveVisaStepDraft(this.enrolment.id, key, this.stepFieldsDraft[key] ?? {}).subscribe({
      next: () => {
        this.isSavingStep = false;
        this.notificationService.success('Draft saved.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingStep = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this step.'));
      }
    });
  }

  completeStep(key: VisaStepKey): void {
    if (!this.canEdit() || !this.canCompleteStep(key)) return;
    this.isCompletingStep = true;
    this.enrolmentService.completeVisaStep(this.enrolment.id, key, this.stepFieldsDraft[key] ?? {}).subscribe({
      next: () => {
        this.isCompletingStep = false;
        this.editingVisaOutcome = false;
        this.notificationService.success('Step marked complete.');
        const nextIndex = this.stepOrder.indexOf(key) + 1;
        this.openStepKey = nextIndex < this.stepOrder.length ? this.stepOrder[nextIndex] : null;
        this.changed.emit();
      },
      error: (err) => {
        this.isCompletingStep = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not complete this step.'));
      }
    });
  }

}
