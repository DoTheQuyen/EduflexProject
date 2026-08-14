import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Client } from '@services/api.services';
import { MigrationCaseService } from '@services/migration-case.service';
import { EmailTemplateService } from '@services/email-template.service';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DocumentUploaderZoneComponent, UploaderZoneFile } from '@generic/document-uploader-zone/document-uploader-zone.component';
import { GenericDocumentsTabComponent, DocumentCategoryDef } from '@generic/generic-documents-tab/generic-documents-tab.component';
import { GenericAuditTabComponent } from '@generic/generic-audit-tab/generic-audit-tab.component';
import { GenericCommunicationTabComponent, CommunicationRecipientOption, CommunicationAttachableDocument, SendCommunicationPayload } from '@generic/generic-communication-tab/generic-communication-tab.component';
import { GenericFormsTabComponent, SaveFormAnswersPayload } from '@generic/generic-forms-tab/generic-forms-tab.component';
import { TaskListComponent } from '@generic/task-list/task-list.component';
import { Address, EmergencyContact, EmailTemplate } from '@app/models/enrolment';
import { DynamicFormTemplate, FormResponseStatus } from '@app/models/dynamic-form';
import {
  MigrationCase,
  MigrationCaseStep,
  UpdateCustomerInfoRequest,
  caseStatusBadgeClass,
  stepStatusBadgeClass
} from '@app/models/migration-case';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';

type CaseTab = 'customer' | 'process' | 'documents' | 'communication' | 'forms' | 'tasks' | 'audit';

@Component({
  selector: 'app-migration-case-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, DocumentUploaderZoneComponent, GenericDocumentsTabComponent, GenericAuditTabComponent, GenericCommunicationTabComponent, GenericFormsTabComponent, TaskListComponent],
  templateUrl: './migration-case-detail.component.html',
  styleUrls: ['./migration-case-detail.component.css']
})
export class MigrationCaseDetailComponent implements OnInit {
  caseId!: string;
  migrationCase: MigrationCase | null = null;
  isLoading = false;
  permissions!: ModulePermissions;
  currentUserId = '';
  activeTab: CaseTab = 'process';

  // ----- Process tab state -----
  drafts: Record<string, Record<string, string>> = {};
  openSteps: Record<string, boolean> = {};
  openTips: Record<string, boolean> = {};
  isSavingStep: Record<string, boolean> = {};
  isUploadingEvidence: Record<string, boolean> = {};

  // ----- Customer Info tab state -----
  customerForm: UpdateCustomerInfoRequest = { primaryContactName: '' };
  isSavingCustomer = false;

  // ----- Documents tab state -----
  isUploadingDocument = false;

  // ----- Communication tab state -----
  templates: EmailTemplate[] = [];
  isSendingCommunication = false;

  // ----- Forms tab state -----
  activeFormTemplates: DynamicFormTemplate[] = [];
  isFormsBusy = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private apiClient: Client,
    private caseService: MigrationCaseService,
    private emailTemplateService: EmailTemplateService,
    private formTemplateService: DynamicFormTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasMigrationCasesPermission();
    this.currentUserId = this.authHelper.getCurrentUser()?.id ?? '';
  }

  ngOnInit(): void {
    this.caseId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
    this.emailTemplateService.getAll().subscribe({
      next: (templates) => { this.templates = templates.filter(t => t.isActive); },
      error: () => {}
    });
    this.formTemplateService.getAll().subscribe({
      next: (templates) => { this.activeFormTemplates = templates.filter(t => t.status === 'Active'); },
      error: () => {}
    });
  }

  load(): void {
    this.isLoading = true;
    this.caseService.getById(this.caseId).subscribe({
      next: (c) => {
        this.migrationCase = c;
        this.drafts = {};
        c.steps.forEach((s) => (this.drafts[s.key] = { ...s.fields }));
        const current = c.steps.find((s) => s.status === 'Draft');
        if (current) this.openSteps[current.key] = true;
        this.loadCustomerFormFrom(c);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this case.');
      }
    });
  }

  get isOwner(): boolean {
    return !!this.migrationCase && this.migrationCase.ownerUserId === this.currentUserId;
  }

  get canManage(): boolean {
    return this.isOwner && this.permissions.edit;
  }

  switchTab(tab: CaseTab): void {
    this.activeTab = tab;
  }

  caseBadge(): string {
    return this.migrationCase ? caseStatusBadgeClass(this.migrationCase.status) : '';
  }

  // ===================== Customer Info tab =====================

  private loadCustomerFormFrom(c: MigrationCase): void {
    this.customerForm = {
      primaryContactName: c.primaryContactName,
      primaryContactEmail: c.primaryContactEmail ?? undefined,
      primaryContactMobile: c.primaryContactMobile ?? undefined,
      middleName: c.middleName ?? undefined,
      dateOfBirth: c.dateOfBirth ?? undefined,
      gender: c.gender ?? undefined,
      nationality: c.nationality ?? undefined,
      passportNumber: c.passportNumber ?? undefined,
      hometownAddress: c.hometownAddress ? { ...c.hometownAddress } : this.blankAddress(),
      currentAddress: c.currentAddress ? { ...c.currentAddress } : this.blankAddress(),
      emergencyContact: c.emergencyContact ? { ...c.emergencyContact } : this.blankEmergencyContact()
    };
  }

  private blankAddress(): Address {
    return { street: '', suburb: '', city: '', state: '', country: '', postalCode: '' };
  }

  private blankEmergencyContact(): EmergencyContact {
    return { name: '', relationship: '', phone: '', email: '' };
  }

  saveCustomerInfo(): void {
    if (!this.migrationCase || !this.customerForm.primaryContactName.trim()) {
      this.notificationService.error('Primary contact name is required.');
      return;
    }
    this.isSavingCustomer = true;
    this.caseService.updateCustomerInfo(this.migrationCase.id, this.customerForm).subscribe({
      next: () => {
        this.isSavingCustomer = false;
        this.notificationService.success('Customer info saved.');
        this.load();
      },
      error: (err) => {
        this.isSavingCustomer = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save customer info.'));
      }
    });
  }

  // ===================== Process tab =====================

  get phases(): string[] {
    if (!this.migrationCase) return [];
    const seen: string[] = [];
    this.migrationCase.steps.forEach((s) => {
      const phase = s.phase || 'Other';
      if (!seen.includes(phase)) seen.push(phase);
    });
    return seen;
  }

  stepsForPhase(phase: string): MigrationCaseStep[] {
    return this.migrationCase?.steps.filter((s) => (s.phase || 'Other') === phase) ?? [];
  }

  stepBadge(step: MigrationCaseStep): string {
    return stepStatusBadgeClass(step.status);
  }

  toggleStep(step: MigrationCaseStep): void {
    if (step.status === 'Locked') return;
    this.openSteps[step.key] = !this.openSteps[step.key];
  }

  toggleTips(step: MigrationCaseStep): void {
    this.openTips[step.key] = !this.openTips[step.key];
  }

  canEditStep(step: MigrationCaseStep): boolean {
    return step.status !== 'Locked' && this.canManage;
  }

  saveDraft(step: MigrationCaseStep): void {
    if (!this.migrationCase) return;
    this.isSavingStep[step.key] = true;
    this.caseService.saveStepDraft(this.migrationCase.id, step.key, this.drafts[step.key]).subscribe({
      next: () => {
        this.isSavingStep[step.key] = false;
        this.notificationService.success('Draft saved.');
        this.load();
      },
      error: (err) => {
        this.isSavingStep[step.key] = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this draft.'));
      }
    });
  }

  completeStep(step: MigrationCaseStep): void {
    if (!this.migrationCase) return;
    const missingRequired = step.fieldsSnapshot.some((f) => f.isRequired && !(this.drafts[step.key][f.fieldKey] ?? '').trim());
    if (missingRequired) {
      this.notificationService.error('Fill in every required field before marking this step complete.');
      return;
    }

    this.isSavingStep[step.key] = true;
    this.caseService.completeStep(this.migrationCase.id, step.key, this.drafts[step.key]).subscribe({
      next: () => {
        this.isSavingStep[step.key] = false;
        this.notificationService.success(`Step "${step.label}" completed.`);
        this.load();
      },
      error: (err) => {
        this.isSavingStep[step.key] = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not complete this step.'));
      }
    });
  }

  reopenStep(step: MigrationCaseStep): void {
    if (!this.migrationCase) return;
    this.caseService.reopenStep(this.migrationCase.id, step.key).subscribe({
      next: () => {
        this.notificationService.success(`Step "${step.label}" reopened.`);
        this.load();
      },
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not reopen this step.'));
      }
    });
  }

  evidenceFor(category: string): UploaderZoneFile[] {
    return (this.migrationCase?.documents ?? [])
      .filter((d) => d.category === category)
      .map((d) => ({ id: d.id, fileName: d.fileName, url: d.url, uploadedByName: d.uploadedByName, uploadedAt: d.uploadedAt }));
  }

  onEvidenceFileSelected(category: string, file: File): void {
    if (!this.migrationCase) return;
    this.isUploadingEvidence[category] = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.caseService.addDocument(this.migrationCase!.id, {
          fileName: result.fileName ?? file.name,
          category,
          url: result.url ?? '',
          contentType: file.type,
          sizeBytes: file.size
        }).subscribe({
          next: () => {
            this.isUploadingEvidence[category] = false;
            this.notificationService.success('Document uploaded.');
            this.load();
          },
          error: (err) => {
            this.isUploadingEvidence[category] = false;
            this.notificationService.error(extractHttpErrorMessage(err, 'Could not attach the uploaded file.'));
          }
        });
      },
      error: () => {
        this.isUploadingEvidence[category] = false;
        this.notificationService.error('File upload failed. Please try again.');
      }
    });
  }

  // ===================== Documents tab =====================

  // Every category any step on this case can require evidence for, deduplicated — a
  // Migration Case has no one fixed category list the way Enrolment does (each template
  // defines its own), so this is computed from the case's own step snapshots.
  get documentCategories(): DocumentCategoryDef[] {
    if (!this.migrationCase) return [];
    const seen = new Set<string>();
    const defs: DocumentCategoryDef[] = [];
    for (const step of this.migrationCase.steps) {
      for (const key of step.requiredEvidenceCategoriesSnapshot) {
        if (!seen.has(key)) {
          seen.add(key);
          defs.push({ key, label: key });
        }
      }
    }
    return defs;
  }

  onDocumentFileSelected(file: File): void {
    if (!this.migrationCase) return;
    this.isUploadingDocument = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.caseService.addDocument(this.migrationCase!.id, {
          fileName: result.fileName ?? file.name,
          category: 'Other',
          url: result.url ?? '',
          contentType: file.type,
          sizeBytes: file.size
        }).subscribe({
          next: () => {
            this.isUploadingDocument = false;
            this.notificationService.success('Document uploaded.');
            this.load();
          },
          error: (err) => {
            this.isUploadingDocument = false;
            this.notificationService.error(extractHttpErrorMessage(err, 'Could not attach the uploaded file.'));
          }
        });
      },
      error: () => {
        this.isUploadingDocument = false;
        this.notificationService.error('File upload failed. Please try again.');
      }
    });
  }

  onDocumentRename(event: { documentId: string; newFileName: string; note?: string }): void {
    // Migration Case has no rename endpoint (backend §MigrationCasesController never
    // needed one, since evidence documents are added with their real filename and rarely
    // renamed) — declared here only so the generic component's output has somewhere to
    // go; surfacing a clear message rather than silently doing nothing.
    this.notificationService.error('Renaming documents is not available on Migration Cases yet.');
  }

  onDocumentDelete(documentId: string): void {
    if (!this.migrationCase) return;
    this.caseService.deleteDocument(this.migrationCase.id, documentId).subscribe({
      next: () => {
        this.notificationService.success('Document removed.');
        this.load();
      },
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not remove this document.'));
      }
    });
  }

  // ===================== Communication tab =====================

  // Only "Primary Contact" is a resolvable recipient here — a generic case has no
  // education/business-partner concept the way Enrolment does (see MigrationCaseModel's
  // comment on why a second party is captured as step Fields, not a structured contact).
  // "Other" leaves the To field free-text for anything else (e.g. the sponsor named on a
  // Partner-visa step).
  get communicationRecipientOptions(): CommunicationRecipientOption[] {
    if (!this.migrationCase) return [];
    return [
      { value: 'PrimaryContact', label: 'Primary Contact', email: this.migrationCase.primaryContactEmail ?? undefined },
      { value: 'Other', label: 'Other' }
    ];
  }

  get communicationAttachableDocuments(): CommunicationAttachableDocument[] {
    return (this.migrationCase?.documents ?? []).map(d => ({ id: d.id, fileName: d.fileName, categoryLabel: d.category ?? undefined }));
  }

  onSendCommunication(payload: SendCommunicationPayload): void {
    if (!this.migrationCase) return;
    this.isSendingCommunication = true;
    this.caseService.sendCommunication(
      this.migrationCase.id, payload.toEmail, payload.recipientType, payload.subject, payload.body,
      payload.attachedDocumentIds, payload.templateKey
    ).subscribe({
      next: () => {
        this.isSendingCommunication = false;
        this.notificationService.success('Email sent.');
        this.load();
      },
      error: (err) => {
        this.isSendingCommunication = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this email. Please try again.'));
      }
    });
  }

  // ===================== Forms tab =====================
  // allowStaffSubmit=true here (unlike Enrolment's forms-tab wrapper) — a Migration Case
  // contact has no portal account, so staff both request AND fill in the form via the
  // generic component's "Submit" button, which routes to MigrationCaseService.
  // SaveFormAnswersAsync(markSubmitted: true) — see that method's comment.

  onRequestMigrationForm(formTemplateId: string): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.requestForm(this.migrationCase.id, formTemplateId).subscribe({
      next: () => { this.isFormsBusy = false; this.notificationService.success('Form requested.'); this.load(); },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not request this form.')); }
    });
  }

  onSaveFormAnswers(payload: SaveFormAnswersPayload): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.saveFormAnswers(this.migrationCase.id, payload.responseId, payload.answers, payload.markSubmitted).subscribe({
      next: () => {
        this.isFormsBusy = false;
        this.notificationService.success(payload.markSubmitted ? 'Form submitted.' : 'Draft saved.');
        this.load();
      },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not save these answers.')); }
    });
  }

  onWithdrawForm(responseId: string): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.withdrawFormRequest(this.migrationCase.id, responseId).subscribe({
      next: () => { this.isFormsBusy = false; this.notificationService.success('Request withdrawn.'); this.load(); },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not withdraw this request.')); }
    });
  }

  onArchiveForm(responseId: string): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.archiveFormResponse(this.migrationCase.id, responseId).subscribe({
      next: () => { this.isFormsBusy = false; this.notificationService.success('Request archived.'); this.load(); },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not archive this request.')); }
    });
  }

  onReopenForm(responseId: string): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.reopenFormForEdit(this.migrationCase.id, responseId).subscribe({
      next: () => { this.isFormsBusy = false; this.notificationService.success('Form reopened for edit.'); this.load(); },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not reopen this form.')); }
    });
  }

  onSetFormStatus(event: { responseId: string; status: FormResponseStatus }): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.setFormResponseStatus(this.migrationCase.id, event.responseId, event.status).subscribe({
      next: () => { this.isFormsBusy = false; this.notificationService.success('Status updated.'); this.load(); },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, "Could not update this form's status.")); }
    });
  }

  onExportFormPdf(responseId: string): void {
    if (!this.migrationCase) return;
    this.isFormsBusy = true;
    this.caseService.exportForm(this.migrationCase.id, responseId).subscribe({
      next: (blob) => {
        this.isFormsBusy = false;
        const response = this.migrationCase!.formResponses.find(r => r.id === responseId);
        const fileName = `${response?.formName ?? 'form'}-${this.migrationCase!.primaryContactName}-${formatDateTime(new Date().toISOString(), 'dd MMM yyyy')}.pdf`;
        this.triggerFormDownload(blob, fileName);
      },
      error: (err) => { this.isFormsBusy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not export this form.')); }
    });
  }

  onOpenFormDocument(exportedDocumentId: string): void {
    const doc = this.migrationCase?.documents.find(d => d.id === exportedDocumentId);
    if (doc) { window.open(doc.url, '_blank'); }
  }

  private triggerFormDownload(blob: Blob, fileName: string): void {
    const objectUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(objectUrl);
  }

  // ===================== Tasks tab =====================
  // app-task-list (generic-components/task-list) is used directly in the template —
  // already built generic ('linked' mode + LinkedRecordType), no wrapper needed here.

  onViewTask(taskId: string): void {
    this.router.navigate(['/staff-portal/tasks', taskId]);
  }

  onAddTask(): void {
    if (!this.migrationCase) return;
    this.router.navigate(['/staff-portal/tasks/new'], { queryParams: { migrationCaseId: this.migrationCase.id } });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/migration-cases']);
  }
}
