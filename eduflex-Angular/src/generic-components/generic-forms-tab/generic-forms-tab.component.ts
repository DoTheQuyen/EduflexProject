import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { FormAnswerEditorComponent } from '@generic/form-answer-editor/form-answer-editor.component';
import { ModalComponent } from '@generic/modal/modal.component';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { DynamicFormTemplate, EnrolmentFormResponse, FormAnswer, FormResponseStatus, formResponseStatusBadgeClass } from '@app/models/dynamic-form';

type ConfirmActionType = 'withdraw' | 'archive' | 'reopen';

interface PendingConfirmAction {
  type: ConfirmActionType;
  response: EnrolmentFormResponse;
}

type FormsScope = 'active' | 'archived';

export interface SaveFormAnswersPayload {
  responseId: string;
  answers: FormAnswer[];
  markSubmitted: boolean;
}

// Fully generic "forms" tab — no knowledge of Enrolment or MigrationCase, no API calls of
// its own; the caller supplies its own response list + active templates and owns every
// request. allowStaffSubmit toggles the one real behavioral difference between the two
// callers: Enrolment's forms are filled in by the student via their own portal (this tab
// only ever corrects already-submitted answers, status untouched), while a Migration Case
// contact has no portal account, so staff fill the form in themselves and need a way to
// finalize it — see MigrationCaseService.SaveFormAnswersAsync's comment for the backend
// side of this split.
@Component({
  selector: 'app-generic-forms-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, FormPrintPreviewComponent, FormAnswerEditorComponent, ModalComponent],
  templateUrl: './generic-forms-tab.component.html',
  styleUrls: ['./generic-forms-tab.component.css']
})
export class GenericFormsTabComponent implements OnChanges {
  @Input({ required: true }) responses: EnrolmentFormResponse[] = [];
  @Input() activeTemplates: DynamicFormTemplate[] = [];
  @Input() canManage = false;
  @Input() allowStaffSubmit = false;
  @Input() contactName = '';
  @Input() contactEmail = '';
  @Input() contactMobile = '';
  @Input() busy = false;
  @Input() noPermissionMessage = 'You do not have permission to manage forms here.';
  @Input() notOwnerMessage = 'Only the staff member who owns this record can manage its forms.';

  @Output() requestForm = new EventEmitter<string>();
  @Output() saveAnswers = new EventEmitter<SaveFormAnswersPayload>();
  @Output() withdraw = new EventEmitter<string>();
  @Output() archive = new EventEmitter<string>();
  @Output() reopen = new EventEmitter<string>();
  @Output() setStatus = new EventEmitter<{ responseId: string; status: FormResponseStatus }>();
  @Output() exportPdf = new EventEmitter<string>();
  @Output() openDocument = new EventEmitter<string>();

  selectedResponseId: string | null = null;
  scope: FormsScope = 'active';

  showRequestPicker = false;
  templateIdToRequest = '';

  editingResponseId: string | null = null;
  editAnswers: FormAnswer[] = [];

  pendingConfirm: PendingConfirmAction | null = null;

  readonly allStatuses: FormResponseStatus[] = ['Requesting', 'Draft', 'Responded', 'Withdrawn', 'Archived'];
  statusOverrideResponse: EnrolmentFormResponse | null = null;
  statusOverrideValue: FormResponseStatus | '' = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['responses']) return;
    if (!this.selectedResponseId || !this.visibleResponses.some(r => r.id === this.selectedResponseId)) {
      this.selectedResponseId = this.visibleResponses[0]?.id ?? null;
    }
  }

  get sortedResponses(): EnrolmentFormResponse[] {
    return [...this.responses].sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime());
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

  openRequestPicker(): void {
    if (!this.canManage) return;
    this.templateIdToRequest = '';
    this.showRequestPicker = true;
  }

  submitRequest(): void {
    if (!this.templateIdToRequest) return;
    this.requestForm.emit(this.templateIdToRequest);
    this.showRequestPicker = false;
  }

  // ----- Withdraw / Archive / Reopen — routed through one styled confirm dialog. -----

  askWithdraw(response: EnrolmentFormResponse): void {
    if (!this.canManage) return;
    this.pendingConfirm = { type: 'withdraw', response };
  }

  askArchive(response: EnrolmentFormResponse): void {
    if (!this.canManage) return;
    this.pendingConfirm = { type: 'archive', response };
  }

  askReopen(response: EnrolmentFormResponse): void {
    if (!this.canManage) return;
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
      case 'withdraw': return `Withdraw the request for "${formName}"?`;
      case 'archive': return `Archive the withdrawn request for "${formName}"? This form can then be requested again.`;
      case 'reopen': return `Reopen "${formName}" for edit? Its status moves back to Requesting.`;
      default: return '';
    }
  }

  cancelConfirm(): void {
    if (this.busy) return;
    this.pendingConfirm = null;
  }

  runConfirm(): void {
    if (!this.pendingConfirm) return;
    const { type, response } = this.pendingConfirm;
    if (type === 'withdraw') this.withdraw.emit(response.id);
    else if (type === 'archive') this.archive.emit(response.id);
    else this.reopen.emit(response.id);
    if (type === 'archive') { this.scope = 'archived'; }
    this.pendingConfirm = null;
  }

  // ----- Manual status override -----

  openStatusOverride(response: EnrolmentFormResponse): void {
    if (!this.canManage) return;
    this.statusOverrideResponse = response;
    this.statusOverrideValue = response.status;
  }

  cancelStatusOverride(): void {
    if (this.busy) return;
    this.statusOverrideResponse = null;
    this.statusOverrideValue = '';
  }

  saveStatusOverride(): void {
    if (!this.statusOverrideResponse || !this.statusOverrideValue) return;
    this.setStatus.emit({ responseId: this.statusOverrideResponse.id, status: this.statusOverrideValue });
    this.statusOverrideResponse = null;
    this.statusOverrideValue = '';
  }

  startEditResponse(response: EnrolmentFormResponse): void {
    if (!this.canManage) return;
    this.editingResponseId = response.id;
    this.editAnswers = response.answers.map(a => ({ ...a, selectedOptions: [...a.selectedOptions] }));
  }

  cancelEditResponse(): void {
    this.editingResponseId = null;
    this.editAnswers = [];
  }

  saveEditResponse(markSubmitted: boolean): void {
    if (!this.editingResponseId) return;
    this.saveAnswers.emit({ responseId: this.editingResponseId, answers: this.editAnswers, markSubmitted });
  }

  exportResponsePdf(response: EnrolmentFormResponse): void {
    this.exportPdf.emit(response.id);
  }

  openLatestDocument(response: EnrolmentFormResponse): void {
    if (!response.exportedDocumentId) return;
    this.openDocument.emit(response.exportedDocumentId);
  }
}
