import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Client, EducationPartnerDto, CourseDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { ModalComponent } from '@generic/modal/modal.component';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { DynamicFormTemplate } from '@app/models/dynamic-form';
import {
  Enrolment, EnrolmentDocument,
  VisaStepKey, VisaProcessStep, VISA_STEP_ORDER, VISA_STEP_EVIDENCE_CATEGORY, VISA_STEP_LABELS
} from '../../../../../../../models/enrolment';

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
  imports: [CommonModule, ReactiveFormsModule, FormsModule, FileUploaderComponent, ModalComponent, FormPrintPreviewComponent],
  templateUrl: './visa-process-tab.component.html',
  styleUrls: ['./visa-process-tab.component.css']
})
export class VisaProcessTabComponent implements OnInit, OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() partners: EducationPartnerDto[] = [];
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  readonly stepOrder = VISA_STEP_ORDER;
  readonly stepLabels = VISA_STEP_LABELS;
  readonly stepDescriptions = STEP_DESCRIPTIONS;
  readonly stepEvidenceCategory = VISA_STEP_EVIDENCE_CATEGORY;

  editingStudent = false;
  editingEnrolment = false;
  studentForm: FormGroup;
  enrolmentForm: FormGroup;
  isSaving = false;

  courses: CourseDto[] = [];

  // VISA Outcome is the one step that can still be corrected after being marked
  // Complete (the backend allows re-completing it specifically) — this toggle re-opens
  // its fields for editing, mirroring the editingStudent/editingEnrolment pattern above.
  editingVisaOutcome = false;

  openStepKey: VisaStepKey | null = null;
  stepFieldsDraft: Partial<Record<VisaStepKey, Record<string, string>>> = {};
  isSavingStep = false;
  isCompletingStep = false;
  isUploadingStepEvidence: Partial<Record<VisaStepKey, boolean>> = {};
  private hasInitializedOpenStep = false;
  private lastCourseLookupPartnerId: string | undefined;

  // ----- Dynamic Forms — bound-step marker + preview/request popup -----
  activeTemplates: DynamicFormTemplate[] = [];
  formPopupStepKey: VisaStepKey | null = null;
  isRequestingBoundForm = false;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private dynamicFormTemplateService: DynamicFormTemplateService,
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
      educationPartnerId: [''], courseId: [''], intake: [''], studyMode: [''], campus: [''],
      commencementDate: [''], actualCommencementDate: [''], expectedCompletionDate: [''], fundingSource: [''], visaStatus: [''],
      status: [''], notes: [''], tuitionFee: [null as number | null]
    });
  }

  ngOnInit(): void {
    this.dynamicFormTemplateService.getAll().subscribe({
      next: (templates) => { this.activeTemplates = templates.filter(t => t.status === 'Active'); },
      error: () => {}
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

    if (this.enrolment.educationPartnerId && this.enrolment.educationPartnerId !== this.lastCourseLookupPartnerId) {
      this.lastCourseLookupPartnerId = this.enrolment.educationPartnerId;
      this.apiClient.byPartner(this.enrolment.educationPartnerId).subscribe({
        next: (courses) => { this.courses = courses; },
        error: () => {}
      });
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
      educationPartnerId: e.educationPartnerId ?? '', courseId: e.courseId ?? '', intake: e.intake ?? '',
      studyMode: e.studyMode ?? '', campus: e.campus ?? '',
      commencementDate: e.commencementDate ? e.commencementDate.substring(0, 10) : '',
      actualCommencementDate: e.actualCommencementDate ? e.actualCommencementDate.substring(0, 10) : '',
      expectedCompletionDate: e.expectedCompletionDate ? e.expectedCompletionDate.substring(0, 10) : '',
      fundingSource: e.fundingSource ?? '', visaStatus: e.visaStatus ?? '', status: e.status, notes: e.notes ?? '',
      tuitionFee: e.tuitionFee ?? null
    });
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  onPartnerChange(): void {
    const partnerId = this.enrolmentForm.value.educationPartnerId;
    this.enrolmentForm.patchValue({ courseId: '' });
    this.courses = [];
    if (!partnerId) return;
    this.apiClient.byPartner(partnerId).subscribe({
      next: (courses) => { this.courses = courses; },
      error: () => {}
    });
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
      educationPartnerId: en.educationPartnerId || undefined, courseId: en.courseId || undefined, intake: en.intake || undefined,
      studyMode: en.studyMode || undefined, campus: en.campus || undefined,
      commencementDate: en.commencementDate || undefined, actualCommencementDate: en.actualCommencementDate || undefined,
      expectedCompletionDate: en.expectedCompletionDate || undefined,
      fundingSource: en.fundingSource || undefined, visaStatus: en.visaStatus || undefined,
      status: en.status, notes: en.notes || undefined, tuitionFee: en.tuitionFee ?? undefined
    };

    this.isSaving = true;
    this.enrolmentService.update(this.enrolment.id, payload).subscribe({
      next: () => {
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

  canCompleteStep(key: VisaStepKey): boolean {
    const category = this.stepEvidenceCategory[key];
    const hasEvidence = !category || this.stepEvidenceDocuments(key).length > 0;
    if (key === 'VisaOutcome') {
      const outcome = this.fieldValue('VisaOutcome', 'outcome');
      return hasEvidence && (outcome === 'Granted' || outcome === 'Refused');
    }
    return hasEvidence;
  }

  toggleEditVisaOutcome(): void {
    if (!this.canEdit()) return;
    this.editingVisaOutcome = !this.editingVisaOutcome;
  }

  onStepEvidenceSelected(key: VisaStepKey, file: File): void {
    const category = this.stepEvidenceCategory[key];
    if (!category) return;
    this.isUploadingStepEvidence[key] = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.enrolmentService.addDocument(this.enrolment.id, {
          fileName: result.fileName ?? file.name,
          category,
          url: result.url ?? '',
          contentType: file.type,
          sizeBytes: file.size
        }).subscribe({
          next: () => {
            this.isUploadingStepEvidence[key] = false;
            this.notificationService.success('Evidence uploaded.');
            this.changed.emit();
          },
          error: (err) => {
            this.isUploadingStepEvidence[key] = false;
            this.notificationService.error(extractHttpErrorMessage(err, 'Could not attach the uploaded file.'));
          }
        });
      },
      error: () => {
        this.isUploadingStepEvidence[key] = false;
        this.notificationService.error('File upload failed. Please try again.');
      }
    });
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

  get partnerName(): string {
    const id = this.enrolment.educationPartnerId;
    if (!id) return '—';
    return this.partners.find(p => p.id === id)?.name ?? '—';
  }

  get courseName(): string {
    const id = this.enrolment.courseId;
    if (!id) return '—';
    return this.courses.find(c => c.id === id)?.courseName ?? '—';
  }
}
