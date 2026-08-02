import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EducationPartnerDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { EmailTemplateService } from '@services/email-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EmailTemplate, RecipientType, documentCategoryLabel } from '../../../../../../../models/enrolment';

@Component({
  selector: 'app-communication-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './communication-tab.component.html',
  styleUrls: ['./communication-tab.component.css']
})
export class CommunicationTabComponent implements OnInit {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() partners: EducationPartnerDto[] = [];
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  templates: EmailTemplate[] = [];
  showComposeForm = false;
  recipientType: RecipientType = 'Student';
  commToEmail = '';
  selectedTemplateKey = '';
  commSubject = '';
  commBody = '';
  selectedAttachmentIds: string[] = [];
  isSending = false;

  constructor(
    private enrolmentService: EnrolmentService,
    private emailTemplateService: EmailTemplateService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.emailTemplateService.getAll().subscribe({
      next: (templates) => { this.templates = templates.filter(t => t.isActive); },
      error: () => {}
    });
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  categoryLabel(category?: string): string {
    return documentCategoryLabel(category);
  }

  toggleCompose(): void {
    this.showComposeForm = !this.showComposeForm;
    if (this.showComposeForm && !this.commToEmail) {
      this.setRecipientType('Student');
    }
  }

  get selectedPartner(): EducationPartnerDto | undefined {
    return this.partners.find(p => p.id === this.enrolment.educationPartnerId);
  }

  setRecipientType(type: RecipientType): void {
    this.recipientType = type;
    if (type === 'Student') {
      this.commToEmail = this.enrolment.email;
    } else if (type === 'EducationPartner') {
      this.commToEmail = this.selectedPartner?.email ?? '';
    } else {
      this.commToEmail = '';
    }
  }

  applyTemplate(template: EmailTemplate): void {
    this.selectedTemplateKey = template.key;
    this.commSubject = template.subject;
    this.commBody = template.body;
  }

  toggleAttachment(docId: string): void {
    const idx = this.selectedAttachmentIds.indexOf(docId);
    if (idx >= 0) { this.selectedAttachmentIds.splice(idx, 1); }
    else { this.selectedAttachmentIds.push(docId); }
  }

  isAttachmentSelected(docId: string): boolean {
    return this.selectedAttachmentIds.includes(docId);
  }

  sendEmail(): void {
    if (!this.commToEmail.trim() || !this.commSubject.trim() || !this.commBody.trim()) {
      this.notificationService.error('Recipient, subject and message are all required.');
      return;
    }
    this.isSending = true;
    this.enrolmentService.sendCommunication(
      this.enrolment.id, this.commToEmail.trim(), this.recipientType, this.commSubject, this.commBody,
      this.selectedAttachmentIds, this.selectedTemplateKey || undefined
    ).subscribe({
      next: () => {
        this.isSending = false;
        this.notificationService.success('Email sent.');
        this.commSubject = '';
        this.commBody = '';
        this.selectedTemplateKey = '';
        this.selectedAttachmentIds = [];
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
