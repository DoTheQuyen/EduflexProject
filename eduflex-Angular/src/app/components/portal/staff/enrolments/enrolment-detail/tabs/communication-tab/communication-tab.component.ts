import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EducationPartnerDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { EmailTemplateService } from '@services/email-template.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { GenericCommunicationTabComponent, CommunicationRecipientOption, CommunicationAttachableDocument, SendCommunicationPayload } from '@generic/generic-communication-tab/generic-communication-tab.component';
import { Enrolment, EmailTemplate, RecipientType, documentCategoryLabel } from '../../../../../../../models/enrolment';

// Enrolment-specific wrapper around the fully generic app-generic-communication-tab —
// supplies Enrolment's fixed Student/EducationPartner/BusinessPartner recipient chips
// (with the education partner's email pre-resolved) and wires sending to
// EnrolmentService. Same wrapper pattern as documents-tab.component.ts.
@Component({
  selector: 'app-communication-tab',
  standalone: true,
  imports: [CommonModule, GenericCommunicationTabComponent],
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

  get selectedPartner(): EducationPartnerDto | undefined {
    return this.partners.find(p => p.id === this.enrolment.educationPartnerId);
  }

  get recipientOptions(): CommunicationRecipientOption[] {
    return [
      { value: 'Student', label: 'Student', email: this.enrolment.email },
      {
        value: 'EducationPartner', label: 'Education Partner', email: this.selectedPartner?.email,
        helpText: this.selectedPartner
          ? `${this.selectedPartner.name}${this.selectedPartner.phoneNumber ? ' · Phone: ' + this.selectedPartner.phoneNumber : ''}`
          : 'No university selected on the Enrolment Form step yet.'
      },
      { value: 'BusinessPartner', label: 'Business Partner', helpText: 'No business-partner directory yet — enter the contact email manually below.' }
    ];
  }

  get attachableDocuments(): CommunicationAttachableDocument[] {
    return this.enrolment.documents.map(d => ({ id: d.id, fileName: d.fileName, categoryLabel: documentCategoryLabel(d.category) }));
  }

  get canCompose(): boolean {
    return this.isOwner && this.permissions.edit;
  }

  onSend(payload: SendCommunicationPayload): void {
    this.isSending = true;
    this.enrolmentService.sendCommunication(
      this.enrolment.id, payload.toEmail, payload.recipientType as RecipientType, payload.subject, payload.body,
      payload.attachedDocumentIds, payload.templateKey
    ).subscribe({
      next: () => {
        this.isSending = false;
        this.notificationService.success('Email sent.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSending = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not send this email. Please try again.'));
      }
    });
  }
}
