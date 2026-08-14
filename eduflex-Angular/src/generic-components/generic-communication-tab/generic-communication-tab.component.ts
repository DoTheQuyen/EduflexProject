import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { formatDateTime } from '@app/shared/utils/date-time.util';

export interface GenericCommunication {
  id: string;
  templateKey?: string | null;
  toEmail: string;
  recipientType: string;
  subject: string;
  body: string;
  attachedDocumentIds: string[];
  sentByName: string;
  sentAt: string;
}

// A chip in the recipient-type toggle. email pre-fills the To field when the caller
// already knows it (e.g. Enrolment resolving its education partner's email); leave it
// undefined for a type with no fixed address, and the field stays free-text.
export interface CommunicationRecipientOption {
  value: string;
  label: string;
  email?: string;
  helpText?: string;
}

export interface CommunicationTemplate {
  key: string;
  name: string;
  subject: string;
  body: string;
}

export interface CommunicationAttachableDocument {
  id: string;
  fileName: string;
  categoryLabel?: string;
}

export interface SendCommunicationPayload {
  toEmail: string;
  recipientType: string;
  subject: string;
  body: string;
  attachedDocumentIds: string[];
  templateKey?: string;
}

// Fully generic "communication" tab — no knowledge of Enrolment, MigrationCase, students,
// or education/business partners. The caller supplies its own recipient-type chips
// (with any pre-resolved email address per type) and its own document/template lists;
// this component only composes the message and emits it. Same zero-backend-coupling
// convention as GenericDocumentsTabComponent/GenericAuditTabComponent — the caller owns
// the actual API call and the append to its own communications list.
@Component({
  selector: 'app-generic-communication-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './generic-communication-tab.component.html',
  styleUrls: ['./generic-communication-tab.component.css']
})
export class GenericCommunicationTabComponent {
  @Input({ required: true }) communications: GenericCommunication[] = [];
  @Input({ required: true }) recipientOptions: CommunicationRecipientOption[] = [];
  @Input() documents: CommunicationAttachableDocument[] = [];
  @Input() templates: CommunicationTemplate[] = [];
  @Input() canCompose = false;
  @Input() isSending = false;
  @Input() composeIntro = 'Compose a message.';

  @Output() send = new EventEmitter<SendCommunicationPayload>();

  showComposeForm = false;
  recipientType = '';
  toEmail = '';
  selectedTemplateKey = '';
  subject = '';
  body = '';
  selectedAttachmentIds: string[] = [];

  toggleCompose(): void {
    this.showComposeForm = !this.showComposeForm;
    if (this.showComposeForm && !this.recipientType && this.recipientOptions.length > 0) {
      this.setRecipientType(this.recipientOptions[0].value);
    }
  }

  get selectedRecipientOption(): CommunicationRecipientOption | undefined {
    return this.recipientOptions.find((o) => o.value === this.recipientType);
  }

  setRecipientType(value: string): void {
    this.recipientType = value;
    this.toEmail = this.recipientOptions.find((o) => o.value === value)?.email ?? '';
  }

  applyTemplate(template: CommunicationTemplate): void {
    this.selectedTemplateKey = template.key;
    this.subject = template.subject;
    this.body = template.body;
  }

  toggleAttachment(docId: string): void {
    const idx = this.selectedAttachmentIds.indexOf(docId);
    if (idx >= 0) { this.selectedAttachmentIds.splice(idx, 1); }
    else { this.selectedAttachmentIds.push(docId); }
  }

  isAttachmentSelected(docId: string): boolean {
    return this.selectedAttachmentIds.includes(docId);
  }

  get canSend(): boolean {
    return this.toEmail.trim().length > 0 && this.subject.trim().length > 0 && this.body.trim().length > 0;
  }

  sendEmail(): void {
    if (!this.canSend) return;
    this.send.emit({
      toEmail: this.toEmail.trim(),
      recipientType: this.recipientType,
      subject: this.subject,
      body: this.body,
      attachedDocumentIds: this.selectedAttachmentIds,
      templateKey: this.selectedTemplateKey || undefined
    });
    this.subject = '';
    this.body = '';
    this.selectedTemplateKey = '';
    this.selectedAttachmentIds = [];
    this.showComposeForm = false;
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }
}
