import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FinancialRecordService } from '@services/financial-record.service';
import { EmailTemplateService } from '@services/email-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { EmailTemplate } from '../../../../../../../models/enrolment';
import { FinancialRecipientType, FinancialRecord } from '../../../../../../../models/financial-record';

// Copy of Enrolment's communication-tab component (per the codebase's per-domain-copy
// convention, not a shared component) — the Student recipient option is removed, and an
// invoice picker lets staff attach a generated invoice's download link to the email.
@Component({
  selector: 'app-financial-communication-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './communication-tab.component.html',
  styleUrls: ['./communication-tab.component.css']
})
export class FinancialCommunicationTabComponent implements OnInit {
  @Input({ required: true }) record!: FinancialRecord;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  templates: EmailTemplate[] = [];
  showComposeForm = false;
  recipientType: FinancialRecipientType = 'EducationPartner';
  commToEmail = '';
  selectedTemplateKey = '';
  commSubject = '';
  commBody = '';
  selectedInvoiceId = '';
  isSending = false;

  constructor(
    private financialRecordService: FinancialRecordService,
    private emailTemplateService: EmailTemplateService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.emailTemplateService.getAll().subscribe({
      next: (templates) => { this.templates = templates; },
      error: () => {}
    });
  }

  get generatedInvoices() {
    return this.record.invoices.filter(i => i.status === 'Generated');
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  toggleCompose(): void {
    this.showComposeForm = !this.showComposeForm;
    if (this.showComposeForm && !this.commToEmail) {
      this.setRecipientType('EducationPartner');
    }
  }

  setRecipientType(type: FinancialRecipientType): void {
    this.recipientType = type;
    this.commToEmail = '';
  }

  applyTemplate(template: EmailTemplate): void {
    this.selectedTemplateKey = template.key;
    this.commSubject = template.subject;
    this.commBody = template.body;
  }

  onInvoiceSelected(): void {
    if (!this.selectedInvoiceId) return;
    const invoice = this.record.invoices.find(i => i.id === this.selectedInvoiceId);
    if (invoice) {
      this.commBody += `\n\n[Invoice ${invoice.invoiceNo} will be attached as a download link.]`;
    }
  }

  sendEmail(): void {
    if (!this.commToEmail.trim() || !this.commSubject.trim() || !this.commBody.trim()) {
      this.notificationService.error('Recipient, subject and message are all required.');
      return;
    }
    this.isSending = true;
    this.financialRecordService.sendCommunication(
      this.record.id, this.commToEmail.trim(), this.recipientType, this.commSubject, this.commBody,
      this.selectedTemplateKey || undefined, this.selectedInvoiceId || undefined
    ).subscribe({
      next: () => {
        this.isSending = false;
        this.notificationService.success('Email sent.');
        this.commSubject = '';
        this.commBody = '';
        this.selectedTemplateKey = '';
        this.selectedInvoiceId = '';
        this.showComposeForm = false;
        this.changed.emit();
      },
      error: (err) => {
        this.isSending = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this email. Please try again.'));
      }
    });
  }
}
