import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AddressDto, Client, CourseDto, CreateApplicationDto, EducationPartnerDto, EmergencyContactDto, SettingsDto, UploadLimitDto } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { EnrolmentService } from '@services/enrolment.service';
import { ModalComponent } from '@generic/modal/modal.component';
import { FormAnswerEditorComponent } from '@generic/form-answer-editor/form-answer-editor.component';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { formatDateTime } from '@app/shared/utils/date-time.util';
// import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { EnrolmentFormResponse, FormAnswer, formResponseStatusBadgeClass } from '@app/models/dynamic-form';
import { MyEnrolmentSummary, ENROLMENT_STAGE_STEPS, enrolmentStageIndex, isEnrolmentStopped } from '@app/models/enrolment';

interface DocFile {
  name: string;
  type: string;
  note: string;
}

interface DocSectionDef {
  key: string;
  label: string;
  icon: string;
  max: number;
  stage: 'initial' | 'later';
  fixedType: boolean;
  options: string[];
}

// Once decided (Approved/Rejected) or actually enrolled (Studying), the
// application record itself is frozen — no more edits.
const LOCKED_STATUSES = ['Approved', 'Rejected', 'Studying'];

const DOC_DEFS: DocSectionDef[] = [
  { key: 'passport', label: 'Passport', icon: 'fa-passport', max: 1, stage: 'initial', fixedType: true, options: ['Passport'] },
  { key: 'ielts', label: 'IELTS Result', icon: 'fa-headphones', max: 1, stage: 'initial', fixedType: true, options: ['IELTS Result'] },
  { key: 'cert', label: 'Year 10-12 Certificate', icon: 'fa-graduation-cap', max: 1, stage: 'initial', fixedType: true, options: ['Year 10-12 Certificate'] },
  { key: 'degree', label: 'Highest Qualification', icon: 'fa-award', max: 1, stage: 'initial', fixedType: false, options: ['College Diploma', 'Bachelor Degree', 'Master Degree'] },
  { key: 'gss', label: 'Genuine Student Statement', icon: 'fa-file-signature', max: 1, stage: 'later', fixedType: true, options: ['Genuine Student Statement'] },
  { key: 'financial', label: 'Financial Evidence', icon: 'fa-sack-dollar', max: 1, stage: 'later', fixedType: true, options: ['Financial Evidence'] },
  { key: 'other', label: 'Other Supporting Documents', icon: 'fa-folder-open', max: 4, stage: 'later', fixedType: false,
    options: ['Bank Statement', 'Sponsorship Letter', 'Health Insurance', 'Reference Letter', 'Resume / CV', 'Other'] },
];

function emptyDocs(): Record<string, DocFile[]> {
  const docs: Record<string, DocFile[]> = {};
  DOC_DEFS.forEach(def => docs[def.key] = []);
  return docs;
}

// Used only until the real limits load from GET /api/settings (or if that call fails),
// so the form still has something sane to validate against rather than nothing at all.
const FALLBACK_UPLOAD_LIMIT: UploadLimitDto = new UploadLimitDto({
  maxSizeMB: 5,
  allowedExtensions: ['.pdf', '.jpg', '.jpeg', '.png'],
  maxFileCount: 1
});

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './application-detail.component.html',
  styleUrls: ['./application-detail.component.css']
})
export class ApplicationDetailComponent implements OnInit {
  readonly docDefs = DOC_DEFS;
  readonly requiredKeys = DOC_DEFS.filter(d => d.stage === 'initial').map(d => d.key);

  userInfo: any;

  get isStaffPortal(): boolean {
    return this.router.url.startsWith('/staff-portal');
  }
  mode: 'new' | 'edit' | 'view' = 'new';
  appId: string | null = null;
  status = 'Pending';
  dateApplied: Date | null = null;
  isLoading = false;
  isSaving = false;

  partners: EducationPartnerDto[] = [];
  university = '';
  course = '';
  studyMode = '';
  campus = '';
  description = '';
  docs: Record<string, DocFile[]> = emptyDocs();

  // These reflect the student's situation for THIS application specifically, so
  // they live on the application itself rather than being read from a shared
  // student profile — the same person could give a different current address or
  // emergency contact on a different application.
  hometownStreet = '';
  hometownSuburb = '';
  hometownCity = '';
  hometownState = '';
  hometownPostcode = '';
  hometownCountry = '';

  currentStreet = '';
  currentSuburb = '';
  currentCity = '';
  currentState = '';
  currentPostcode = '';
  currentCountry = '';

  sameAsHometown = false;

  emergencyName = '';
  emergencyRelationship = '';
  emergencyPhone = '';
  emergencyEmail = '';

  get availableCourses(): string[] {
    const partner = this.partners.find(p => p.name === this.university);
    return partner?.courses?.map(c => c.courseName!).filter(Boolean) ?? [];
  }

  private get selectedCourseDto(): CourseDto | undefined {
    const partner = this.partners.find(p => p.name === this.university);
    return partner?.courses?.find(c => c.courseName === this.course);
  }

  get availableStudyModes(): string[] {
    return this.selectedCourseDto?.studyModes ?? [];
  }

  get availableCampuses(): string[] {
    return this.selectedCourseDto?.campuses ?? [];
  }

  addFormOpenFor: string | null = null;
  draftType: Record<string, string> = {};
  draftNote: Record<string, string> = {};
  draftFile: Record<string, File | null> = {};

  settings: SettingsDto | null = null;


myEnrolmentId: string | null = null;
myForms: EnrolmentFormResponse[] = [];
mySummary: MyEnrolmentSummary | null = null;
readonly stageSteps = ENROLMENT_STAGE_STEPS;

get currentStageIndex(): number {
  return this.mySummary ? enrolmentStageIndex(this.mySummary.status) : -1;
}

get enrolmentStopped(): boolean {
  return this.mySummary ? isEnrolmentStopped(this.mySummary.status) : false;
}

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private appService: Client,
    private authHelper: AuthHelperService,
    private notification: NotificationService,
    private enrolmentService: EnrolmentService
  ) {}

  ngOnInit(): void {
    this.userInfo = this.authHelper.getCurrentUser();
    this.appService.educationPartnersAll().subscribe({
      next: (partners: EducationPartnerDto[]) => { this.partners = partners ?? []; },
      error: () => { this.notification.error('Could not load the university list.'); }
    });
    this.appService.settingsGET().subscribe({
      next: (settings: SettingsDto) => { this.settings = settings; },
      error: () => { this.notification.error('Could not load upload settings — using fallback limits.'); }
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.mode = 'new';
      return;
    }
    this.appId = id;
    this.loadApplication(id);
  }

  private loadApplication(id: string): void {
    this.isLoading = true;
    this.appService.applicationsGET2(id).subscribe({
      next: (app) => {
        this.status = app.status || 'Pending';
        this.dateApplied = app.dateApplied ?? null;
        this.university = app.applicationType || '';
        this.course = app.description || '';
        this.studyMode = app.studyMode || '';
        this.campus = app.campus || '';
        this.description = app.details || '';

        const home = app.hometownAddress;
        this.hometownStreet = home?.street || '';
        this.hometownSuburb = home?.suburb || '';
        this.hometownCity = home?.city || '';
        this.hometownState = home?.state || '';
        this.hometownPostcode = home?.postalCode || '';
        this.hometownCountry = home?.country || '';

        const current = app.currentAddress;
        this.currentStreet = current?.street || '';
        this.currentSuburb = current?.suburb || '';
        this.currentCity = current?.city || '';
        this.currentState = current?.state || '';
        this.currentPostcode = current?.postalCode || '';
        this.currentCountry = current?.country || '';

        const contact = app.emergencyContact;
        this.emergencyName = contact?.name || '';
        this.emergencyRelationship = contact?.relationship || '';
        this.emergencyPhone = contact?.phone || '';
        this.emergencyEmail = contact?.email || '';

        this.mode = LOCKED_STATUSES.includes(this.status) ? 'view' : 'edit';
        this.isLoading = false;

        if (!this.isStaffPortal) {
          this.loadMyForms(id);
          this.loadMySummary(id);
        }
      },
      error: () => {
        this.notification.error('Could not load this application.');
        this.isLoading = false;
        this.router.navigate([this.isStaffPortal ? '/staff-portal/applications' : '/student-portal/application']);
      }
    });
  }

  get isLocked(): boolean {
    return this.mode === 'view' || LOCKED_STATUSES.includes(this.status);
  }

  get requiredDocsComplete(): boolean {
    return this.requiredKeys.every(key => this.docs[key].length > 0);
  }

  get requiredDocsDoneCount(): number {
    return this.requiredKeys.filter(key => this.docs[key].length > 0).length;
  }

  get formComplete(): boolean {
    return !!this.university && !!this.course.trim() && !!this.description.trim();
  }

  get canSubmit(): boolean {
    return this.mode === 'new' && this.formComplete && this.requiredDocsComplete && !this.isSaving;
  }

  docStatusTag(def: DocSectionDef): string {
    return def.stage === 'initial' ? 'Initial stage' : 'Additional';
  }

  // "other" gets its own configured limit (it already allows several files);
  // every other document slot shares the single "default" limit.
  uploadLimitFor(def: DocSectionDef): UploadLimitDto {
    const upload = this.settings?.documentUpload;
    const limit = def.key === 'other' ? upload?.other : upload?.default;
    return limit ?? FALLBACK_UPLOAD_LIMIT;
  }

  maxFilesFor(def: DocSectionDef): number {
    return this.uploadLimitFor(def).maxFileCount ?? FALLBACK_UPLOAD_LIMIT.maxFileCount!;
  }

  private validateFile(def: DocSectionDef, file: File): string | null {
    const limit = this.uploadLimitFor(def);
    const maxSizeMB = limit.maxSizeMB ?? FALLBACK_UPLOAD_LIMIT.maxSizeMB!;
    const allowedExtensions = limit.allowedExtensions ?? FALLBACK_UPLOAD_LIMIT.allowedExtensions!;

    const extension = file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : '';
    if (!allowedExtensions.some(ext => ext.toLowerCase() === extension)) {
      return `"${file.name}" isn't an allowed file type. Allowed: ${allowedExtensions.join(', ')}.`;
    }

    const maxSizeBytes = maxSizeMB * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      return `"${file.name}" is too large (max ${maxSizeMB}MB).`;
    }

    return null;
  }

  toggleSameAsHometown(): void {
    if (!this.sameAsHometown) return;
    this.currentStreet = this.hometownStreet;
    this.currentSuburb = this.hometownSuburb;
    this.currentCity = this.hometownCity;
    this.currentState = this.hometownState;
    this.currentPostcode = this.hometownPostcode;
    this.currentCountry = this.hometownCountry;
  }

  toggleAddForm(key: string): void {
    this.addFormOpenFor = this.addFormOpenFor === key ? null : key;
    const def = this.docDefs.find(d => d.key === key)!;
    this.draftType[key] = def.fixedType ? def.options[0] : (this.draftType[key] || def.options[0]);
    this.draftNote[key] = this.draftNote[key] || '';
    this.draftFile[key] = null;
  }

  onDraftFileChange(key: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    const def = this.docDefs.find(d => d.key === key)!;

    if (file) {
      const error = this.validateFile(def, file);
      if (error) {
        this.notification.error(error);
        this.draftFile[key] = null;
        input.value = '';
        return;
      }
    }

    this.draftFile[key] = file;
  }

  attachDocument(key: string): void {
    const file = this.draftFile[key];
    if (!file) {
      this.notification.error('Choose a file before attaching.');
      return;
    }
    const def = this.docDefs.find(d => d.key === key)!;
    if (this.docs[key].length >= this.maxFilesFor(def)) {
      this.notification.error(`You can only attach up to ${this.maxFilesFor(def)} file(s) here.`);
      return;
    }
    this.docs[key].push({ name: file.name, type: this.draftType[key], note: (this.draftNote[key] || '').trim() });
    this.draftFile[key] = null;
    this.addFormOpenFor = null;
    this.notification.success('Document attached to this screen. It is not saved to the server yet.');
  }

  removeDocument(key: string, index: number): void {
    this.docs[key].splice(index, 1);
  }

  goBack(): void {
    this.router.navigate([this.isStaffPortal ? '/staff-portal/applications' : '/student-portal/application']);
  }

  cancelUpdate(): void {
    if (this.mode === 'new') {
      const dirty = !!this.university || !!this.course || !!this.description ||
        Object.values(this.docs).some(list => list.length > 0);
      if (dirty && !confirm('Discard this new application? Nothing will be saved.')) return;
      this.router.navigate(['/student-portal/application']);
      return;
    }
    if (!confirm('Discard unsaved changes and reload the last saved version?')) return;
    this.docs = emptyDocs();
    if (this.appId) this.loadApplication(this.appId);
  }

  withdraw(): void {
    this.notification.error("Withdrawing isn't available yet — the backend doesn't support it for students.");
  }

  saveDraft(): void {
    this.notification.error(this.mode === 'new'
      ? "Draft saving isn't wired to the backend yet — use Submit to actually create the application."
      : "Saving changes isn't available yet — the backend doesn't support editing an application after it's created.");
  }

  submit(): void {
    if (this.mode !== 'new') {
      this.notification.error("Editing an existing application isn't available yet — the backend doesn't support it.");
      return;
    }
    if (!this.canSubmit) return;

    this.isSaving = true;
    const dto = new CreateApplicationDto({
      studentId: this.userInfo?.id,
      studentName: `${this.userInfo?.firstName ?? ''} ${this.userInfo?.lastName ?? ''}`.trim(),
      applicationType: this.university,
      description: this.course,
      studyMode: this.studyMode || undefined,
      campus: this.campus || undefined,
      details: this.description,
      hometownAddress: new AddressDto({
        street: this.hometownStreet, suburb: this.hometownSuburb, city: this.hometownCity,
        state: this.hometownState, postalCode: this.hometownPostcode, country: this.hometownCountry
      }),
      currentAddress: new AddressDto({
        street: this.currentStreet, suburb: this.currentSuburb, city: this.currentCity,
        state: this.currentState, postalCode: this.currentPostcode, country: this.currentCountry
      }),
      emergencyContact: new EmergencyContactDto({
        name: this.emergencyName, relationship: this.emergencyRelationship,
        phone: this.emergencyPhone, email: this.emergencyEmail
      })
    });

    this.appService.applicationsPOST(dto).subscribe({
      next: () => {
        this.isSaving = false;
        this.notification.success('Application submitted.');
        this.router.navigate(['/student-portal/application']);
      },
      error: () => {
        this.isSaving = false;
        this.notification.error('Something went wrong submitting the application.');
      }
    });
  }

  formatDate(value: any): string {
    return formatDateTime(value, 'dd MMM yyyy');
  }

  // ===================== Dynamic Forms (student) =====================

  loadMyForms(applicationId: string): void {
    this.enrolmentService.getMyForms(applicationId).subscribe({
      next: (result) => {
        this.myEnrolmentId = result.enrolmentId;
        this.myForms = result.forms;
      },
      // A 404 here just means no Enrolment is linked to this application yet
      // (e.g. still Pending) — not every application has one.
      error: () => { this.myEnrolmentId = null; this.myForms = []; }
    });
  }

  loadMySummary(applicationId: string): void {
    this.enrolmentService.getMySummary(applicationId).subscribe({
      next: (summary) => { this.mySummary = summary; },
      // A 404 here just means no Enrolment is linked to this application yet — same as
      // loadMyForms, not every application has one.
      error: () => { this.mySummary = null; }
    });
  }

  formStatusBadgeClass(form: EnrolmentFormResponse): string {
    return formResponseStatusBadgeClass(form.status);
  }

  isFormLocked(form: EnrolmentFormResponse): boolean {
    return form.status === 'Responded';
  }
  
 openForm(form: EnrolmentFormResponse): void {
    this.router.navigate(['/student-portal/application', this.appId, 'forms', form.id]);
  }
}
