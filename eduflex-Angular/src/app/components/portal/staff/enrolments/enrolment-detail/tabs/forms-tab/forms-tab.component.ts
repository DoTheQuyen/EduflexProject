import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EnrolmentService } from '@services/enrolment.service';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { GenericFormsTabComponent, SaveFormAnswersPayload } from '@generic/generic-forms-tab/generic-forms-tab.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { DynamicFormTemplate, FormResponseStatus } from '@app/models/dynamic-form';
import { Enrolment } from '../../../../../../../models/enrolment';

// Enrolment-specific wrapper around the fully generic app-generic-forms-tab — the student
// fills the form in via their own portal, so this tab only requests forms and lets staff
// correct/withdraw/archive/reopen already-existing responses (allowStaffSubmit=false, so
// the generic component never offers a "Submit" button, only "Save changes" which routes
// to staffEditFormResponse — a status-preserving edit, not a finalize).
@Component({
  selector: 'app-forms-tab',
  standalone: true,
  imports: [CommonModule, GenericFormsTabComponent],
  templateUrl: './forms-tab.component.html',
  styleUrls: ['./forms-tab.component.css']
})
export class FormsTabComponent implements OnInit {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  activeTemplates: DynamicFormTemplate[] = [];
  busy = false;

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

  get canManage(): boolean {
    return this.isOwner && this.permissions.edit;
  }

  private guardOwner(): boolean {
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

  onRequestForm(formTemplateId: string): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.requestForm(this.enrolment.id, formTemplateId).subscribe({
      next: () => {
        this.busy = false;
        this.notificationService.success('Form requested — an email has been sent to the student.');
        this.changed.emit();
      },
      error: (err) => {
        this.busy = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not request this form.'));
      }
    });
  }

  onSaveAnswers(payload: SaveFormAnswersPayload): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.staffEditFormResponse(this.enrolment.id, payload.responseId, payload.answers).subscribe({
      next: () => {
        this.busy = false;
        this.notificationService.success("Student's answers updated.");
        this.changed.emit();
      },
      error: (err) => {
        this.busy = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save these changes.'));
      }
    });
  }

  onWithdraw(responseId: string): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.withdrawFormRequest(this.enrolment.id, responseId).subscribe({
      next: () => { this.busy = false; this.notificationService.success('Request withdrawn.'); this.changed.emit(); },
      error: (err) => { this.busy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not withdraw this request.')); }
    });
  }

  onArchive(responseId: string): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.archiveFormResponse(this.enrolment.id, responseId).subscribe({
      next: () => { this.busy = false; this.notificationService.success('Request archived.'); this.changed.emit(); },
      error: (err) => { this.busy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not archive this request.')); }
    });
  }

  onReopen(responseId: string): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.reopenFormForEdit(this.enrolment.id, responseId).subscribe({
      next: () => { this.busy = false; this.notificationService.success('Form reopened for the student to edit.'); this.changed.emit(); },
      error: (err) => { this.busy = false; this.notificationService.error(extractHttpErrorMessage(err, 'Could not reopen this form.')); }
    });
  }

  onSetStatus(event: { responseId: string; status: FormResponseStatus }): void {
    if (!this.guardOwner()) return;
    this.busy = true;
    this.enrolmentService.setFormResponseStatus(this.enrolment.id, event.responseId, event.status).subscribe({
      next: () => { this.busy = false; this.notificationService.success('Status updated.'); this.changed.emit(); },
      error: (err) => { this.busy = false; this.notificationService.error(extractHttpErrorMessage(err, "Could not update this form's status.")); }
    });
  }

  onExportPdf(responseId: string): void {
    this.busy = true;
    this.enrolmentService.exportForm(this.enrolment.id, responseId).subscribe({
      next: (blob) => {
        this.busy = false;
        const response = this.enrolment.formResponses.find(r => r.id === responseId);
        const studentFullName = `${this.enrolment.firstName} ${this.enrolment.lastName}`.trim();
        const fileName = `${response?.formName ?? 'form'}-${studentFullName}-${formatDateTime(new Date().toISOString(), 'dd MMM yyyy')}.pdf`;
        this.triggerDownload(blob, fileName);
      },
      error: (err) => {
        this.busy = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not export this form.'));
      }
    });
  }

  onOpenDocument(exportedDocumentId: string): void {
    const doc = this.enrolment.documents.find(d => d.id === exportedDocumentId);
    if (doc) { window.open(doc.url, '_blank'); }
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
}
