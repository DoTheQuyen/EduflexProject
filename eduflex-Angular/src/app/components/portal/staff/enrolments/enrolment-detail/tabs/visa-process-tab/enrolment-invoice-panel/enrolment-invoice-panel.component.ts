import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client, InvoiceTemplateDto, SendInvoiceDto, ConfirmInvoicePaymentDto } from '@services/api.services';
import { EmailTemplateService } from '@services/email-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EmailTemplate } from '../../../../../../../../models/enrolment';

// The Enrolment Form step's service-fee invoice: send-to-student panel + payment
// confirmation. Extracted out of visa-process-tab.component to keep that file a
// manageable size — reads/writes the 'EnrolmentForm' step's fields bag directly rather
// than going through the parent's stepFieldsDraft, since these are read-only display
// values refreshed by the parent's (changed) → reload, not locally-edited drafts.
@Component({
  selector: 'app-enrolment-invoice-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent],
  templateUrl: './enrolment-invoice-panel.component.html',
  styleUrls: ['./enrolment-invoice-panel.component.css']
})
export class EnrolmentInvoicePanelComponent {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  invoiceTemplates: InvoiceTemplateDto[] = [];
  emailTemplates: EmailTemplate[] = [];
  showInvoicePanel = false;
  selectedInvoiceTemplateId = '';
  selectedEmailTemplateKey = '';
  invoiceDescription = 'Enrolment Service Fee';
  invoiceAmount: number | null = null;
  invoiceGstRate = 10;
  invoiceEmailSubject = '';
  invoiceEmailBody = '';
  isSendingInvoice = false;
  // A Manager/Admin can unlock a template-configured amount to type a different one —
  // resets to locked every time the template selection changes.
  amountOverrideUnlocked = false;

  showPaymentUploader = false;
  paymentEvidenceUrl: string | null = null;
  isUploadingPaymentEvidence = false;
  isConfirmingPayment = false;

  constructor(
    private apiClient: Client,
    private emailTemplateService: EmailTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {}

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

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  private fieldValue(field: string): string {
    return this.enrolment.visaProcessSteps.find(s => s.key === 'EnrolmentForm')?.fields?.[field] ?? '';
  }

  get invoiceId(): string {
    return this.fieldValue('invoiceId');
  }

  get invoiceStatus(): 'NotSent' | 'Sent' | 'Paid' {
    if (!this.invoiceId) return 'NotSent';
    return this.fieldValue('invoicePaidAt') ? 'Paid' : 'Sent';
  }

  get invoiceSentAt(): string {
    return this.fieldValue('invoiceSentAt');
  }

  get invoicePaidAt(): string {
    return this.fieldValue('invoicePaidAt');
  }

  toggleInvoicePanel(): void {
    if (!this.canEdit()) return;
    this.showInvoicePanel = !this.showInvoicePanel;
    if (this.showInvoicePanel && this.invoiceTemplates.length === 0) {
      this.apiClient.invoiceTemplatesAll().subscribe({
        next: (templates) => { this.invoiceTemplates = templates.filter(t => t.category === 'Customer' && t.isActive); },
        error: () => {}
      });
    }
    if (this.showInvoicePanel && this.emailTemplates.length === 0) {
      // Not "invoice-notification" — that one's worded for partner commission invoices
      // (see migration 027). "student-invoice-notification" (migration 035) is the one
      // meant for this flow, so pick it automatically if it's there.
      this.emailTemplateService.getAll().subscribe({
        next: (templates) => {
          this.emailTemplates = templates.filter(t => t.isActive);
          const preferred = this.emailTemplates.find(t => t.key === 'student-invoice-notification');
          if (preferred) { this.applyEmailTemplate(preferred); }
        },
        error: () => {}
      });
    }
  }

  get isManager(): boolean {
    const role = this.authHelper.getCurrentUser()?.role;
    return role === 'Manager' || role === 'Admin';
  }

  get selectedInvoiceTemplate(): InvoiceTemplateDto | undefined {
    return this.invoiceTemplates.find(t => t.id === this.selectedInvoiceTemplateId);
  }

  get descriptionLocked(): boolean {
    return !!this.selectedInvoiceTemplate?.defaultDescription;
  }

  get gstLocked(): boolean {
    return this.selectedInvoiceTemplate?.defaultGstRatePercent != null;
  }

  get amountLocked(): boolean {
    return this.selectedInvoiceTemplate?.defaultAmount != null && !this.amountOverrideUnlocked;
  }

  // Called on template selection — prefills description/amount/GST from whatever the
  // template configures, and resets any previous Manager override.
  onInvoiceTemplateChange(): void {
    this.amountOverrideUnlocked = false;
    const t = this.selectedInvoiceTemplate;
    if (!t) return;
    if (t.defaultDescription) { this.invoiceDescription = t.defaultDescription; }
    if (t.defaultAmount != null) { this.invoiceAmount = t.defaultAmount; }
    if (t.defaultGstRatePercent != null) { this.invoiceGstRate = t.defaultGstRatePercent; }
  }

  applyEmailTemplate(template: EmailTemplate): void {
    this.selectedEmailTemplateKey = template.key;
    this.invoiceEmailSubject = template.subject;
    this.invoiceEmailBody = template.body
      .replace(/\{\{studentFirstName\}\}/g, this.enrolment.firstName)
      .replace(/\{\{staffName\}\}/g, '');
  }

  get canSendInvoice(): boolean {
    return !!this.selectedInvoiceTemplateId && !!this.invoiceDescription.trim() &&
      !!this.invoiceAmount && this.invoiceAmount > 0 &&
      !!this.invoiceEmailSubject.trim() && !!this.invoiceEmailBody.trim();
  }

  sendInvoice(): void {
    if (!this.canEdit() || !this.canSendInvoice) return;

    this.isSendingInvoice = true;
    this.apiClient.send(new SendInvoiceDto({
      templateId: this.selectedInvoiceTemplateId,
      recipientType: 'Student',
      recipientId: this.enrolment.studentUserId,
      recipientName: `${this.enrolment.firstName} ${this.enrolment.lastName}`.trim(),
      recipientEmail: this.enrolment.email,
      relatedEnrolmentId: this.enrolment.id,
      relatedStepKey: 'EnrolmentForm',
      description: this.invoiceDescription.trim(),
      amount: this.invoiceAmount!,
      gstRatePercent: this.invoiceGstRate,
      emailSubject: this.invoiceEmailSubject.trim(),
      emailBody: this.invoiceEmailBody.trim()
    })).subscribe({
      next: () => {
        this.isSendingInvoice = false;
        this.showInvoicePanel = false;
        this.notificationService.success('Invoice sent to the student.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSendingInvoice = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this invoice.'));
      }
    });
  }

  onPaymentEvidenceSelected(file: File): void {
    this.isUploadingPaymentEvidence = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.isUploadingPaymentEvidence = false;
        this.paymentEvidenceUrl = result.url ?? null;
      },
      error: () => {
        this.isUploadingPaymentEvidence = false;
        this.notificationService.error('Evidence upload failed. Please try again.');
      }
    });
  }

  confirmPayment(): void {
    if (!this.canEdit() || !this.invoiceId) return;

    this.isConfirmingPayment = true;
    this.apiClient.confirmPayment(this.invoiceId, new ConfirmInvoicePaymentDto({
      paymentEvidenceUrl: this.paymentEvidenceUrl ?? undefined
    })).subscribe({
      next: () => {
        this.isConfirmingPayment = false;
        this.showPaymentUploader = false;
        this.notificationService.success('Payment confirmed.');
        this.changed.emit();
      },
      error: (err) => {
        this.isConfirmingPayment = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not confirm payment.'));
      }
    });
  }
}
