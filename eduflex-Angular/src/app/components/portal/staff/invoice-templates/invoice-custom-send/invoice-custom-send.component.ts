import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Client, InvoiceTemplateDto } from '@services/api.services';
import { EmailTemplateService } from '@services/email-template.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { environment } from '@app/environments/environment';
import { EmailTemplate } from '@app/models/enrolment';
import { SendCustomInvoiceRequest } from '@app/models/invoice';

// A free-text, one-off invoice with no backing student/partner record — e.g. ad-hoc
// consulting or services billing. Posts straight to InvoicesController's existing
// POST /send (RecipientType "Custom"), the same generic endpoint the VISA Process tab
// and Finance module's Invoice tab already use, so no new backend surface was needed
// beyond the CustomContent field added for this category. Hand-written via HttpClient
// rather than the NSwag-generated Client's SendInvoiceDto, since that type doesn't carry
// CustomContent until `nswag run` is next executed — same reasoning as
// FinancialRecordService's other hand-written invoice calls.
@Component({
  selector: 'app-invoice-custom-send',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invoice-custom-send.component.html',
  styleUrls: ['./invoice-custom-send.component.css']
})
export class InvoiceCustomSendComponent implements OnInit {
  templates: InvoiceTemplateDto[] = [];
  emailTemplates: EmailTemplate[] = [];
  selectedTemplateId = '';
  selectedEmailTemplateKey = '';

  recipientName = '';
  recipientEmail = '';
  description = 'Custom Invoice';
  amount: number | null = null;
  gstRatePercent = 10;
  customContent = '';
  emailSubject = '';
  emailBody = '';
  isSending = false;

  constructor(
    private client: Client,
    private http: HttpClient,
    private emailTemplateService: EmailTemplateService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.client.invoiceTemplatesAll().subscribe({
      next: (templates) => { this.templates = templates.filter(t => t.category === 'Custom' && t.isActive); },
      error: () => {}
    });
    this.emailTemplateService.getAll().subscribe({
      next: (templates) => { this.emailTemplates = templates.filter(t => t.isActive); },
      error: () => {}
    });
  }

  get selectedTemplate(): InvoiceTemplateDto | undefined {
    return this.templates.find(t => t.id === this.selectedTemplateId);
  }

  onTemplateChange(): void {
    const t = this.selectedTemplate;
    if (!t) return;
    if (t.defaultDescription) { this.description = t.defaultDescription; }
    if (t.defaultAmount != null) { this.amount = t.defaultAmount; }
    if (t.defaultGstRatePercent != null) { this.gstRatePercent = t.defaultGstRatePercent; }
  }

  applyEmailTemplate(template: EmailTemplate): void {
    this.selectedEmailTemplateKey = template.key;
    this.emailSubject = template.subject;
    this.emailBody = template.body;
  }

  get canSend(): boolean {
    return !!this.selectedTemplateId && !!this.recipientName.trim() && !!this.recipientEmail.trim() &&
      !!this.amount && this.amount > 0 && !!this.customContent.trim() &&
      !!this.emailSubject.trim() && !!this.emailBody.trim();
  }

  send(): void {
    if (!this.canSend) return;

    this.isSending = true;
    const payload: SendCustomInvoiceRequest & { recipientType: string } = {
      templateId: this.selectedTemplateId,
      recipientType: 'Custom',
      recipientName: this.recipientName.trim(),
      recipientEmail: this.recipientEmail.trim(),
      description: this.description.trim(),
      amount: this.amount!,
      gstRatePercent: this.gstRatePercent,
      customContent: this.customContent.trim(),
      emailSubject: this.emailSubject.trim(),
      emailBody: this.emailBody.trim()
    };

    this.http.post(`${environment.apiClientUrl}/api/Invoices/send`, payload).subscribe({
      next: () => {
        this.isSending = false;
        this.notificationService.success(`Invoice sent to ${this.recipientName}.`);
        this.goBack();
      },
      error: (err) => {
        this.isSending = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this invoice.'));
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/invoice-templates']);
  }
}
