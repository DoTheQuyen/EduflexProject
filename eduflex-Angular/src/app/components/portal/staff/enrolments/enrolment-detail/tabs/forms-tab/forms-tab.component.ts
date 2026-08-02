import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EnrolmentService } from '@services/enrolment.service';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { FormAnswerEditorComponent } from '@generic/form-answer-editor/form-answer-editor.component';
import { ModalComponent } from '@generic/modal/modal.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { DynamicFormTemplate, EnrolmentFormResponse, FormAnswer, FormResponseStatus, formResponseStatusBadgeClass } from '@app/models/dynamic-form';
import { Enrolment } from '../../../../../../../models/enrolment';

type ConfirmActionType = 'withdraw' | 'archive' | 'reopen';

interface PendingConfirmAction {
  type: ConfirmActionType;
  response: EnrolmentFormResponse;
}

type FormsScope = 'active' | 'archived';

@Component({
  selector: 'app-forms-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, FormPrintPreviewComponent, FormAnswerEditorComponent, ModalComponent],
  templateUrl: './forms-tab.component.html',
  styleUrls: ['./forms-tab.component.css']
})
export class FormsTabComponent implements OnInit, OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  activeTemplates: DynamicFormTemplate[] = [];
  selectedResponseId: string | null = null;
  scope: FormsScope = 'active';

  showRequestPicker = false;
  templateIdToRequest = '';
  isRequesting = false;

  editingResponseId: string | null = null;
  editAnswers: FormAnswer[] = [];
  isSavingEdit = false;
  isExporting = false;

  pendingConfirm: PendingConfirmAction | null = null;
  isConfirmRunning = false;

  readonly allStatuses: FormResponseStatus[] = ['Requesting', 'Draft', 'Responded', 'Withdrawn', 'Archived'];
  statusOverrideResponse: EnrolmentFormResponse | null = null;
  statusOverrideValue: FormResponseStatus | '' = '';
  isSavingStatusOverride = false;

  constructor(
    private enrolmentService: EnrolmentService,
    private templateService: DynamicFormTemplateService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.templateService.getAll().subscribe({
      next: (templates) => { this.activeTemplates = templates.filter(t => t.status === 'Active'); },
      error: () => {}
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['enrolment'] || !this.enrolment) return;
    if (!this.selectedResponseId || !this.visibleResponses.some(r => r.id === this.selectedResponseId)) {
      this.selectedResponseId = this.visibleResponses[0]?.id ?? null;
    }
  }

  get sortedResponses(): EnrolmentFormResponse[] {
    return [...(this.enrolment?.formResponses ?? [])].sort(
      (a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime()
    );
  }

  get activeResponses(): EnrolmentFormResponse[] {
    return this.sortedResponses.filter(r => r.status !== 'Archived');
  }

  get archivedResponses(): EnrolmentFormResponse[] {
    return this.sortedResponses.filter(r => r.status === 'Archived');
  }

  get visibleResponses(): EnrolmentFormResponse[] {
    return this.scope === 'active' ? this.activeResponses : this.archivedResponses;
  }

  get currentResponse(): EnrolmentFormResponse | undefined {
    return this.visibleResponses.find(r => r.id === this.selectedResponseId);
  }

  get anyActionInFlight(): boolean {
    return this.isConfirmRunning || this.isExporting || this.isSavingEdit || this.isRequesting || this.isSavingStatusOverride;
  }

  switchScope(scope: FormsScope): void {
    if (this.scope === scope) return;
    this.scope = scope;
    this.selectedResponseId = this.visibleResponses[0]?.id ?? null;
    this.cancelEditResponse();
  }

  selectResponse(id: string): void {
    this.selectedResponseId = id;
    this.cancelEditResponse();
  }

  statusBadgeClass(response: EnrolmentFormResponse): string {
    return formResponseStatusBadgeClass(response.status);
  }

  formatDate(value?: string): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  get studentFullName(): string {
    return `${this.enrolment.firstName} ${this.enrolment.lastName}`.trim();
  }

  private canEdit(): boolean {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit enrolments.');
      return false;
    }
    if (!this.isOwner) {
      this.notificationService.error('Only the staff member who owns this enrolment can manage its forms. Ask a manager to reassign it to you.');
      return false;
    }
    return true;
  }

  openRequestPicker(): void {
    if (!this.canEdit()) return;
    this.templateIdToRequest = '';
    this.showRequestPicker = true;
  }

  submitRequest(): void {
    if (!this.templateIdToRequest) return;
    this.isRequesting = true;
    this.enrolmentService.requestForm(this.enrolment.id, this.templateIdToRequest).subscribe({
      next: (response) => {
        this.isRequesting = false;
        this.showRequestPicker = false;
        this.scope = 'active';
        this.selectedResponseId = response.id;
        this.notificationService.success('Form requested — an email has been sent to the student.');
        this.changed.emit();
      },
      error: (err) => {
        this.isRequesting = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not request this form.'));
      }
    });
  }

  // ----- Withdraw / Archive / Reopen — routed through one styled confirm dialog
  // (app-modal's own isSaving-driven overlay) instead of window.confirm, which also
  // blocks the panel's other actions while the request is in flight (anyActionInFlight).

  askWithdraw(response: EnrolmentFormResponse): void {
    if (!this.canEdit()) return;
    this.pendingConfirm = { type: 'withdraw', response };
  }

  askArchive(response: EnrolmentFormResponse): void {
    if (!this.canEdit()) return;
    this.pendingConfirm = { type: 'archive', response };
  }

  askReopen(response: EnrolmentFormResponse): void {
    if (!this.canEdit()) return;
    this.pendingConfirm = { type: 'reopen', response };
  }

  get confirmTitle(): string {
    switch (this.pendingConfirm?.type) {
      case 'withdraw': return 'Withdraw request?';
      case 'archive': return 'Archive request?';
      case 'reopen': return 'Reopen for edit?';
      default: return '';
    }
  }

  get confirmMessage(): string {
    const formName = this.pendingConfirm?.response.formName ?? '';
    switch (this.pendingConfirm?.type) {
      case 'withdraw': return `Withdraw the request for "${formName}"? The student will be notified it's no longer required.`;
      case 'archive': return `Archive the withdrawn request for "${formName}"? This form can then be requested again.`;
      case 'reopen': return `Allow the student to edit "${formName}" again? Its status moves back to Requesting and they'll get a fresh request email.`;
      default: return '';
    }
  }

  cancelConfirm(): void {
    if (this.isConfirmRunning) return;
    this.pendingConfirm = null;
  }

  runConfirm(): void {
    if (!this.pendingConfirm) return;
    const { type, response } = this.pendingConfirm;

    const request$ = type === 'withdraw'
      ? this.enrolmentService.withdrawFormRequest(this.enrolment.id, response.id)
      : type === 'archive'
        ? this.enrolmentService.archiveFormResponse(this.enrolment.id, response.id)
        : this.enrolmentService.reopenFormForEdit(this.enrolment.id, response.id);

    this.isConfirmRunning = true;
    request$.subscribe({
      next: () => {
        this.isConfirmRunning = false;
        this.pendingConfirm = null;
        this.notificationService.success(this.confirmSuccessMessage(type));
        if (type === 'archive') { this.scope = 'archived'; }
        this.changed.emit();
      },
      error: (err) => {
        this.isConfirmRunning = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not complete this action.'));
      }
    });
  }

  private confirmSuccessMessage(type: ConfirmActionType): string {
    switch (type) {
      case 'withdraw': return 'Request withdrawn.';
      case 'archive': return 'Request archived.';
      case 'reopen': return 'Form reopened for the student to edit.';
    }
  }

  // ----- Manual status override — an admin escape hatch, separate from the confirm
  // flow above since it needs an extra input (which status to set) before saving.

  openStatusOverride(response: EnrolmentFormResponse): void {
    if (!this.canEdit()) return;
    this.statusOverrideResponse = response;
    this.statusOverrideValue = response.status;
  }

  cancelStatusOverride(): void {
    if (this.isSavingStatusOverride) return;
    this.statusOverrideResponse = null;
    this.statusOverrideValue = '';
  }

  saveStatusOverride(): void {
    if (!this.statusOverrideResponse || !this.statusOverrideValue) return;
    const response = this.statusOverrideResponse;

    this.isSavingStatusOverride = true;
    this.enrolmentService.setFormResponseStatus(this.enrolment.id, response.id, this.statusOverrideValue).subscribe({
      next: () => {
        this.isSavingStatusOverride = false;
        this.statusOverrideResponse = null;
        this.statusOverrideValue = '';
        this.notificationService.success('Status updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingStatusOverride = false;
        this.notificationService.error(extractHttpErrorMessage(err, "Could not update this form's status."));
      }
    });
  }

  startEditResponse(response: EnrolmentFormResponse): void {
    if (!this.canEdit()) return;
    this.editingResponseId = response.id;
    this.editAnswers = response.answers.map(a => ({ ...a, selectedOptions: [...a.selectedOptions] }));
  }

  cancelEditResponse(): void {
    this.editingResponseId = null;
    this.editAnswers = [];
  }

  saveEditResponse(response: EnrolmentFormResponse): void {
    this.isSavingEdit = true;
    this.enrolmentService.staffEditFormResponse(this.enrolment.id, response.id, this.editAnswers).subscribe({
      next: () => {
        this.isSavingEdit = false;
        this.notificationService.success("Student's answers updated.");
        this.cancelEditResponse();
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingEdit = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save these changes.'));
      }
    });
  }

  exportPdf(response: EnrolmentFormResponse): void {
    this.isExporting = true;
    this.enrolmentService.exportForm(this.enrolment.id, response.id).subscribe({
      next: (blob) => {
        this.isExporting = false;
        const fileName = `${response.formName}-${this.studentFullName}-${formatDateTime(new Date().toISOString(), 'dd MMM yyyy')}.pdf`;
        this.triggerDownload(blob, fileName);
      },
      error: (err) => {
        this.isExporting = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not export this form.'));
      }
    });
  }

  // Downloads via a local blob: object URL rather than window.open on a remote URL —
  // the download attribute reliably honors the filename we set here, unlike a
  // cross-origin URL's own Content-Disposition (see EnrolmentService.ExportFormAsync).
  private triggerDownload(blob: Blob, fileName: string): void {
    const objectUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(objectUrl);
  }

  openLatestDocument(response: EnrolmentFormResponse): void {
    const doc = this.enrolment.documents.find(d => d.id === response.exportedDocumentId);
    if (doc) { window.open(doc.url, '_blank'); }
  }
}
