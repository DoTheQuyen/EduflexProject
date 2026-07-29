import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { Client } from '@services/api.services';
import { FinancialRecordService } from '@services/financial-record.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { ModalComponent } from '@generic/modal/modal.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment } from '../../../../../../../models/enrolment';
import { FinancialRecord, Invoice, InvoiceToType } from '../../../../../../../models/financial-record';

interface InvoiceToOption {
  type: InvoiceToType;
  id: string;
  name: string;
  abn?: string;
  acn?: string;
}

@Component({
  selector: 'app-invoice-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule, ModalComponent],
  templateUrl: './invoice-tab.component.html',
  styleUrls: ['./invoice-tab.component.css']
})
export class InvoiceTabComponent implements OnChanges {
  @Input({ required: true }) record!: FinancialRecord;
  @Input() enrolment: Enrolment | null = null;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();
  @Output() goToCommunicationWithInvoice = new EventEmitter<void>();

  invoiceToOption: InvoiceToOption | null = null;
  courseName = '';

  editingInvoiceId: string | null = null;
  invoiceNo = '';
  periodStart = '';
  periodEnd = '';
  periodTotal = 0;
  htmlContent = '';
  isSaving = false;
  isGenerating = false;
  expandedHistoryId: string | null = null;

  showSendPrompt = false;
  private lastGeneratedInvoiceId: string | null = null;

  constructor(
    private apiClient: Client,
    private financialRecordService: FinancialRecordService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['record'] && this.record) {
      this.resolveInvoiceToOption();
    }
    if (changes['enrolment'] && this.enrolment?.educationPartnerId && this.enrolment?.courseId) {
      this.apiClient.educationPartnersGET(this.enrolment.educationPartnerId).subscribe({
        next: (ep) => { this.courseName = (ep.courses ?? []).find(c => c.id === this.enrolment?.courseId)?.courseName ?? ''; },
        error: () => {}
      });
    }
  }

  private resolveInvoiceToOption(): void {
    if (this.record.businessPartnerId) {
      this.apiClient.businessPartnersGET(this.record.businessPartnerId).subscribe({
        next: (bp) => {
          this.invoiceToOption = { type: 'BusinessPartner', id: bp.id ?? '', name: bp.name ?? '', abn: bp.abn ?? undefined, acn: bp.acn ?? undefined };
          if (!this.editingInvoiceId) { this.startNewDraft(); }
        },
        error: () => {}
      });
    } else if (this.record.educationPartnerId) {
      this.apiClient.educationPartnersGET(this.record.educationPartnerId).subscribe({
        next: (ep) => {
          this.invoiceToOption = { type: 'EducationPartner', id: ep.id ?? '', name: ep.name ?? '', abn: ep.abn ?? undefined, acn: ep.acn ?? undefined };
          if (!this.editingInvoiceId) { this.startNewDraft(); }
        },
        error: () => {}
      });
    }
  }

  private get studentName(): string {
    return this.enrolment ? `${this.enrolment.firstName} ${this.enrolment.lastName}`.trim() : '';
  }

  private get totalCommission(): number {
    const adjustments = this.record.extraCommissionAdjustments.reduce((sum, a) => sum + a.amount, 0);
    return this.record.expectedCommission + adjustments;
  }

  private get alreadyInvoicedTotal(): number {
    return this.record.invoices.filter(i => i.status === 'Generated').reduce((sum, i) => sum + i.periodTotal, 0);
  }

  private suggestInvoiceNo(): string {
    const shortId = this.record.id.slice(-6).toUpperCase();
    return `INV-${shortId}-${this.record.invoices.length + 1}`;
  }

  startNewDraft(): void {
    if (!this.invoiceToOption) return;

    const start = this.enrolment?.actualCommencementDate ? this.enrolment.actualCommencementDate.substring(0, 10) : new Date().toISOString().substring(0, 10);
    const end = new Date();
    end.setMonth(end.getMonth() + 1);

    this.editingInvoiceId = null;
    this.invoiceNo = this.suggestInvoiceNo();
    this.periodStart = start;
    this.periodEnd = end.toISOString().substring(0, 10);
    this.periodTotal = Math.max(0, this.totalCommission - this.alreadyInvoicedTotal);
    this.htmlContent = this.buildInvoiceTemplate();
  }

  editInvoice(invoice: Invoice): void {
    if (invoice.status !== 'Draft') return;
    this.editingInvoiceId = invoice.id;
    this.invoiceNo = invoice.invoiceNo;
    this.periodStart = invoice.periodStart.substring(0, 10);
    this.periodEnd = invoice.periodEnd.substring(0, 10);
    this.periodTotal = invoice.periodTotal;
    this.htmlContent = invoice.htmlContent;
  }

  cancel(): void {
    this.startNewDraft();
  }

  private buildInvoiceTemplate(): string {
    const opt = this.invoiceToOption;
    const abnAcnLine = opt?.abn || opt?.acn
      ? `<p style="margin:0;color:#5C6B7A;font-size:12px;">${opt?.abn ? 'ABN ' + opt.abn : ''}${opt?.abn && opt?.acn ? ' &middot; ' : ''}${opt?.acn ? 'ACN ' + opt.acn : ''}</p>`
      : '';

    return `
<div style="font-family:Arial,Helvetica,sans-serif;color:#1B2430;">
  <table width="100%" style="margin-bottom:24px;">
    <tr>
      <td style="vertical-align:top;">
        <h2 style="margin:0;color:#16233A;">Eduflex</h2>
        <p style="margin:0;color:#5C6B7A;font-size:12px;">Commission Invoice</p>
      </td>
      <td style="text-align:right;vertical-align:top;">
        <p style="margin:0;font-size:14px;"><strong>Invoice No:</strong> ${this.invoiceNo}</p>
        <p style="margin:0;font-size:14px;"><strong>Date issued:</strong> ${formatDateTime(new Date().toISOString(), 'dd/MM/yyyy')}</p>
      </td>
    </tr>
  </table>

  <table width="100%" style="margin-bottom:24px;">
    <tr>
      <td style="vertical-align:top;width:50%;">
        <p style="margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;">Invoice to</p>
        <p style="margin:0;font-weight:bold;">${opt?.name ?? ''}</p>
        ${abnAcnLine}
      </td>
      <td style="vertical-align:top;width:50%;text-align:right;">
        <p style="margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;">Billing period</p>
        <p style="margin:0;">${formatDateTime(this.periodStart, 'dd/MM/yyyy')} &ndash; ${formatDateTime(this.periodEnd, 'dd/MM/yyyy')}</p>
      </td>
    </tr>
  </table>

  <table width="100%" style="border-collapse:collapse;margin-bottom:8px;">
    <thead>
      <tr style="background:#F5F6F7;">
        <th style="text-align:left;padding:8px;border:1px solid #DCE0E4;">Student</th>
        <th style="text-align:left;padding:8px;border:1px solid #DCE0E4;">Course</th>
        <th style="text-align:left;padding:8px;border:1px solid #DCE0E4;">Start date</th>
        <th style="text-align:right;padding:8px;border:1px solid #DCE0E4;">Amount</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td style="padding:8px;border:1px solid #DCE0E4;">${this.studentName}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;">${this.courseName}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;">${this.enrolment?.actualCommencementDate ? formatDateTime(this.enrolment.actualCommencementDate, 'dd/MM/yyyy') : ''}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;text-align:right;">${this.periodTotal.toFixed(2)}</td>
      </tr>
    </tbody>
    <tfoot>
      <tr>
        <td colspan="3" style="padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;">Total due</td>
        <td style="padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;">${this.periodTotal.toFixed(2)}</td>
      </tr>
    </tfoot>
  </table>

  <p style="color:#5C6B7A;font-size:12px;margin-top:24px;">Please remit payment within 30 days of the invoice date. Thank you for your continued partnership with Eduflex.</p>
</div>`.trim();
  }

  saveDraft(): void {
    if (!this.invoiceNo.trim() || !this.invoiceToOption) {
      this.notificationService.error('Invoice number and invoice-to party are required.');
      return;
    }

    this.isSaving = true;
    if (this.editingInvoiceId) {
      this.financialRecordService.updateInvoiceDraft(this.record.id, this.editingInvoiceId, this.htmlContent, this.periodTotal).subscribe({
        next: () => {
          this.isSaving = false;
          this.notificationService.success('Draft saved.');
          this.changed.emit();
        },
        error: (err) => {
          this.isSaving = false;
          this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this draft.'));
        }
      });
    } else {
      this.financialRecordService.createInvoiceDraft(this.record.id, {
        invoiceNo: this.invoiceNo,
        invoiceToType: this.invoiceToOption.type,
        invoiceToId: this.invoiceToOption.id,
        invoiceToName: this.invoiceToOption.name,
        studentName: this.studentName,
        periodStart: this.periodStart,
        periodEnd: this.periodEnd,
        periodTotal: this.periodTotal,
        htmlContent: this.htmlContent
      }).subscribe({
        next: (invoice) => {
          this.isSaving = false;
          this.editingInvoiceId = invoice.id;
          this.notificationService.success('Draft saved.');
          this.changed.emit();
        },
        error: (err) => {
          this.isSaving = false;
          this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this draft.'));
        }
      });
    }
  }

  generatePdf(): void {
    if (!this.editingInvoiceId) {
      this.notificationService.error('Save this invoice as a draft first.');
      return;
    }

    this.isGenerating = true;
    this.financialRecordService.generateInvoicePdf(this.record.id, this.editingInvoiceId).subscribe({
      next: (invoice) => {
        this.isGenerating = false;
        this.lastGeneratedInvoiceId = invoice.id;
        this.notificationService.success('Invoice PDF generated.');
        this.showSendPrompt = true;
        this.changed.emit();
      },
      error: (err) => {
        this.isGenerating = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not generate the PDF. Please try again.'));
      }
    });
  }

  confirmSendPrompt(): void {
    this.showSendPrompt = false;
    this.goToCommunicationWithInvoice.emit();
  }

  dismissSendPrompt(): void {
    this.showSendPrompt = false;
  }

  toggleHistoryRow(invoiceId: string): void {
    this.expandedHistoryId = this.expandedHistoryId === invoiceId ? null : invoiceId;
  }

  openDownloadLink(invoiceId: string): void {
    this.financialRecordService.getInvoiceDownloadLink(this.record.id, invoiceId).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  get generatedInvoices(): Invoice[] {
    return this.record.invoices.filter(i => i.status === 'Generated');
  }
}
