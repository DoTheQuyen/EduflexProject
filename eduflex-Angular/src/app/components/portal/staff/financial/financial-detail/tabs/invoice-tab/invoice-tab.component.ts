import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client, InvoiceTemplateDto, ConfirmInvoicePaymentDto } from '@services/api.services';
import { FinancialRecordService } from '@services/financial-record.service';
import { EmailTemplateService } from '@services/email-template.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EmailTemplate } from '../../../../../../../models/enrolment';
import { FinancialRecord, FinanceInvoice, InvoiceToType } from '../../../../../../../models/financial-record';
import { InvoiceClaimItem } from '../../../../../../../models/invoice';
import { ClaimInvoiceRequest } from '../finance-tab/finance-tab.component';

interface InvoiceToOption {
  type: InvoiceToType;
  id: string;
  name: string;
  email: string;
  abn?: string;
  acn?: string;
}

// Replaces the old free-text Quill-editor draft/generate/manual-email flow with the
// same templated one-step "pick template -> fill claim items -> send" flow already used
// for student service-fee invoices (see EnrolmentInvoicePanelComponent) — sending IS the
// email action now, so there's no more "generate PDF, then go compose an email in the
// Communication tab" handoff. Claim items (plural) replace the old single amount, and a
// student/enrolment summary is sent alongside so the recipient partner can see which
// enrolment the commission relates to (see InvoiceService.BuildStudentBlockHtml).
@Component({
  selector: 'app-invoice-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent],
  templateUrl: './invoice-tab.component.html',
  styleUrls: ['./invoice-tab.component.css']
})
export class InvoiceTabComponent implements OnChanges {
  @Input({ required: true }) record!: FinancialRecord;
  @Input() enrolment: Enrolment | null = null;
  @Input({ required: true }) permissions!: ModulePermissions;
  // Set by FinancialDetailComponent when staff click "Send Invoice" on a specific claim
  // in the Finance tab's invoice request calendar — prefills the claim item below and, on
  // send, flags which InvoicePlan entry this invoice fulfils (see activePlanEntryId).
  @Input() pendingClaim: ClaimInvoiceRequest | null = null;

  invoiceToOption: InvoiceToOption | null = null;
  invoices: FinanceInvoice[] = [];
  isLoadingInvoices = false;

  courseName = '';
  educationPartnerName = '';

  invoiceTemplates: InvoiceTemplateDto[] = [];
  emailTemplates: EmailTemplate[] = [];
  selectedInvoiceTemplateId = '';
  selectedEmailTemplateKey = '';
  claimItems: InvoiceClaimItem[] = [];
  invoiceEmailSubject = '';
  invoiceEmailBody = '';
  isSendingInvoice = false;

  expandedHistoryId: string | null = null;
  showPaymentUploaderFor: string | null = null;
  paymentEvidenceUrl: string | null = null;
  isUploadingPaymentEvidence = false;
  isConfirmingPayment = false;

  resendDialogInvoice: FinanceInvoice | null = null;
  resendSubject = '';
  resendBody = '';
  isResending = false;

  cancelDialogInvoice: FinanceInvoice | null = null;
  cancelReason = '';
  isCancelling = false;

  activePlanEntryId: string | null = null;

  private templatesLoaded = false;

  constructor(
    private apiClient: Client,
    private financialRecordService: FinancialRecordService,
    private emailTemplateService: EmailTemplateService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['record'] && this.record) {
      this.resolveInvoiceToOption();
      this.loadInvoices();
      if (!this.templatesLoaded) {
        this.templatesLoaded = true;
        this.loadTemplates();
      }
    }
    if (changes['enrolment'] && this.enrolment?.educationPartnerId) {
      this.apiClient.educationPartnersGET(this.enrolment.educationPartnerId).subscribe({
        next: (ep) => {
          this.educationPartnerName = ep.name ?? '';
          this.courseName = (ep.courses ?? []).find(c => c.id === this.enrolment?.courseId)?.courseName ?? '';
        },
        error: () => {}
      });
    }
    if (changes['pendingClaim'] && this.pendingClaim) {
      this.activePlanEntryId = this.pendingClaim.entryId;
      this.claimItems = [{ description: this.pendingClaim.description, amount: this.pendingClaim.amount, gstRatePercent: 10 }];
    }
  }

  private resolveInvoiceToOption(): void {
    if (this.record.businessPartnerId) {
      this.apiClient.businessPartnersGET(this.record.businessPartnerId).subscribe({
        next: (bp) => {
          this.invoiceToOption = { type: 'BusinessPartner', id: bp.id ?? '', name: bp.name ?? '', email: bp.email ?? '', abn: bp.abn ?? undefined, acn: bp.acn ?? undefined };
        },
        error: () => {}
      });
    } else if (this.record.educationPartnerId) {
      this.apiClient.educationPartnersGET(this.record.educationPartnerId).subscribe({
        next: (ep) => {
          this.invoiceToOption = { type: 'EducationPartner', id: ep.id ?? '', name: ep.name ?? '', email: ep.email ?? '', abn: ep.abn ?? undefined, acn: ep.acn ?? undefined };
        },
        error: () => {}
      });
    }
  }

  private loadInvoices(): void {
    this.isLoadingInvoices = true;
    this.financialRecordService.getInvoicesForRecord(this.record.id).subscribe({
      next: (invoices) => {
        this.invoices = invoices;
        this.isLoadingInvoices = false;
      },
      error: () => { this.isLoadingInvoices = false; }
    });
  }

  private loadTemplates(): void {
    this.apiClient.invoiceTemplatesAll().subscribe({
      next: (templates) => { this.invoiceTemplates = templates.filter(t => t.category === 'Partner' && t.isActive); },
      error: () => {}
    });
    // "invoice-notification" (migration 027) is worded for partner commission invoices —
    // distinct from "student-invoice-notification" (migration 035), which the VISA
    // Process tab picks instead. No client-side token substitution needed: unlike the
    // enrolment panel's {{studentFirstName}}/{{staffName}}, this template only uses
    // {{invoiceNo}}/{{invoiceLink}}, both resolved server-side in SendInvoiceAsync.
    this.emailTemplateService.getAll().subscribe({
      next: (templates) => {
        this.emailTemplates = templates.filter(t => t.isActive);
        const preferred = this.emailTemplates.find(t => t.key === 'invoice-notification');
        if (preferred) { this.applyEmailTemplate(preferred); }
      },
      error: () => {}
    });
  }

  private get totalCommission(): number {
    const adjustments = this.record.extraCommissionAdjustments.reduce((sum, a) => sum + a.amount, 0);
    return this.record.expectedCommission + adjustments;
  }

  // Mirrors InvoiceStatuses.CountsAsIncome() server-side — Failed and Cancelled invoices
  // must never inflate what's already been billed.
  private get alreadyInvoicedTotal(): number {
    return this.invoices.filter(i => i.status === 'Sent' || i.status === 'Paid').reduce((sum, i) => sum + i.amount, 0);
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Paid': return 'badge-pill-success-soft';
      case 'Sent': return 'badge-pill-navy-soft';
      case 'Failed': return 'badge-pill-error-soft';
      default: return 'badge-pill-muted-soft';
    }
  }

  get selectedInvoiceTemplate(): InvoiceTemplateDto | undefined {
    return this.invoiceTemplates.find(t => t.id === this.selectedInvoiceTemplateId);
  }

  // Seeds one claim-item row from the template's defaults (if any) the first time a
  // template is picked — a starting point only, every row stays freely editable
  // afterward since a fixed "locked" value doesn't make sense once there can be several.
  onInvoiceTemplateChange(): void {
    if (this.claimItems.length > 0) return;
    const t = this.selectedInvoiceTemplate;
    this.claimItems = [{
      description: t?.defaultDescription || 'Enrolment Service Fee',
      amount: t?.defaultAmount ?? Math.max(0, this.totalCommission - this.alreadyInvoicedTotal),
      gstRatePercent: t?.defaultGstRatePercent ?? 10
    }];
  }

  addClaimItem(): void {
    this.claimItems.push({ description: '', amount: 0, gstRatePercent: 10 });
  }

  removeClaimItem(index: number): void {
    this.claimItems.splice(index, 1);
  }

  get claimItemsAmount(): number {
    return this.claimItems.reduce((sum, i) => sum + (i.amount || 0), 0);
  }

  get claimItemsGst(): number {
    return this.claimItems.reduce((sum, i) => sum + Math.round((i.amount || 0) * (i.gstRatePercent || 0)) / 100, 0);
  }

  get claimItemsTotal(): number {
    return this.claimItemsAmount + this.claimItemsGst;
  }

  applyEmailTemplate(template: EmailTemplate): void {
    this.selectedEmailTemplateKey = template.key;
    this.invoiceEmailSubject = template.subject;
    this.invoiceEmailBody = template.body;
  }

  get canSendInvoice(): boolean {
    return !!this.invoiceToOption?.email && !!this.selectedInvoiceTemplateId &&
      this.claimItems.length > 0 && this.claimItems.every(i => !!i.description.trim() && i.amount > 0) &&
      !!this.invoiceEmailSubject.trim() && !!this.invoiceEmailBody.trim();
  }

  sendInvoice(): void {
    if (!this.permissions.edit || !this.invoiceToOption || !this.canSendInvoice) return;

    this.isSendingInvoice = true;
    this.financialRecordService.sendInvoice({
      templateId: this.selectedInvoiceTemplateId,
      recipientType: this.invoiceToOption.type,
      recipientId: this.invoiceToOption.id,
      recipientName: this.invoiceToOption.name,
      recipientEmail: this.invoiceToOption.email,
      relatedFinancialRecordId: this.record.id,
      relatedInvoicePlanEntryId: this.activePlanEntryId ?? undefined,
      description: '',
      amount: 0,
      gstRatePercent: 0,
      claimItems: this.claimItems.map(i => ({ description: i.description.trim(), amount: i.amount, gstRatePercent: i.gstRatePercent })),
      studentName: this.enrolment ? `${this.enrolment.firstName} ${this.enrolment.lastName}`.trim() : undefined,
      studentRefCode: this.enrolment ? this.enrolment.id.slice(-6).toUpperCase() : undefined,
      studentEmail: this.enrolment?.email,
      studentPhone: this.enrolment?.mobile,
      courseName: this.courseName || undefined,
      campus: this.enrolment?.campus,
      educationPartnerName: this.educationPartnerName || undefined,
      emailSubject: this.invoiceEmailSubject.trim(),
      emailBody: this.invoiceEmailBody.trim()
    }).subscribe({
      next: (invoice) => {
        this.isSendingInvoice = false;
        this.claimItems = [];
        this.activePlanEntryId = null;
        // The invoice + PDF are saved before the email goes out, so a failed email comes
        // back as a successful 200 carrying status "Failed" rather than an HTTP error —
        // surface that as a warning with a Resend hint, not a success.
        if (invoice?.status === 'Failed') {
          this.notificationService.error(
            `Invoice ${invoice.invoiceNo} was created but the email could not be sent. Use Resend in the invoice history to retry, or Cancel it.`);
        } else {
          this.notificationService.success(`Invoice sent to ${this.invoiceToOption?.name}.`);
        }
        this.loadInvoices();
      },
      error: (err) => {
        this.isSendingInvoice = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this invoice.'));
      }
    });
  }

  openResendDialog(invoice: FinanceInvoice): void {
    if (!this.permissions.edit) return;
    this.resendDialogInvoice = invoice;
    // Prefill with the message this invoice was last sent with. Invoices raised before
    // the message was persisted have none, so fall back to the partner-invoice email
    // template rather than opening an empty box.
    const fallback = this.emailTemplates.find(t => t.key === 'invoice-notification');
    this.resendSubject = invoice.emailSubject || fallback?.subject || '';
    this.resendBody = invoice.emailBody || fallback?.body || '';
  }

  closeResendDialog(): void {
    this.resendDialogInvoice = null;
    this.resendSubject = '';
    this.resendBody = '';
  }

  applyResendTemplate(template: EmailTemplate): void {
    this.resendSubject = template.subject;
    this.resendBody = template.body;
  }

  get canResend(): boolean {
    return !!this.resendSubject.trim() && !!this.resendBody.trim();
  }

  confirmResend(): void {
    const invoice = this.resendDialogInvoice;
    if (!invoice || !this.canResend) return;

    this.isResending = true;
    this.financialRecordService.resendInvoice(invoice.id, this.resendSubject.trim(), this.resendBody.trim()).subscribe({
      next: () => {
        this.isResending = false;
        this.closeResendDialog();
        this.notificationService.success(`Invoice ${invoice.invoiceNo} resent to ${invoice.recipientEmail}.`);
        this.loadInvoices();
      },
      error: (err) => {
        this.isResending = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not resend this invoice.'));
      }
    });
  }

  openCancelDialog(invoice: FinanceInvoice): void {
    if (!this.permissions.edit) return;
    this.cancelDialogInvoice = invoice;
    this.cancelReason = '';
  }

  closeCancelDialog(): void {
    this.cancelDialogInvoice = null;
    this.cancelReason = '';
  }

  confirmCancel(): void {
    const invoice = this.cancelDialogInvoice;
    if (!invoice) return;

    this.isCancelling = true;
    this.financialRecordService.cancelInvoice(invoice.id, this.cancelReason.trim()).subscribe({
      next: () => {
        this.isCancelling = false;
        this.closeCancelDialog();
        this.notificationService.success(`Invoice ${invoice.invoiceNo} cancelled — it no longer counts toward income.`);
        this.loadInvoices();
      },
      error: (err) => {
        this.isCancelling = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not cancel this invoice.'));
      }
    });
  }

  toggleHistoryRow(invoiceId: string): void {
    this.expandedHistoryId = this.expandedHistoryId === invoiceId ? null : invoiceId;
  }

  openDownloadLink(invoice: FinanceInvoice): void {
    this.apiClient.downloadLink(invoice.id).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  togglePaymentUploader(invoiceId: string): void {
    this.showPaymentUploaderFor = this.showPaymentUploaderFor === invoiceId ? null : invoiceId;
    this.paymentEvidenceUrl = null;
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

  confirmPayment(invoiceId: string): void {
    if (!this.permissions.edit) return;

    this.isConfirmingPayment = true;
    this.apiClient.confirmPayment(invoiceId, new ConfirmInvoicePaymentDto({
      paymentEvidenceUrl: this.paymentEvidenceUrl ?? undefined
    })).subscribe({
      next: () => {
        this.isConfirmingPayment = false;
        this.showPaymentUploaderFor = null;
        this.notificationService.success('Payment confirmed.');
        this.loadInvoices();
      },
      error: (err) => {
        this.isConfirmingPayment = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not confirm payment.'));
      }
    });
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }
}
