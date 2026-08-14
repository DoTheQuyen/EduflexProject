import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Client, InvoiceRecordDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { GenericDocumentsTabComponent, DocumentCategoryDef, GenericDocument } from '@generic/generic-documents-tab/generic-documents-tab.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, DocumentCategory, DOCUMENT_CATEGORY_LABELS } from '../../../../../../../models/enrolment';

const DOCUMENT_STEP_CATEGORIES: DocumentCategory[] = [
  'GS', 'UniOffer', 'CoE', 'PaymentReceipt', 'VisaDraft', 'Insurance', 'VisaPaymentReceipt', 'VisaGranted'
];

// Enrolment-specific wrapper around the fully generic app-generic-documents-tab — supplies
// Enrolment's fixed category list + wires upload/rename/delete to EnrolmentService, and
// composes the invoice list (an Enrolment-only concept — Migration Case has no invoicing)
// alongside it. See documents-tab.component.html's own comment for why invoices ended up
// as a separate section rather than merged into "Other documents" the way they used to be.
@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [CommonModule, GenericDocumentsTabComponent],
  templateUrl: './documents-tab.component.html',
  styleUrls: ['./documents-tab.component.css']
})
export class DocumentsTabComponent implements OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  readonly categories: DocumentCategoryDef[] = DOCUMENT_STEP_CATEGORIES.map((key) => ({
    key, label: DOCUMENT_CATEGORY_LABELS[key]
  }));

  isUploading = false;
  invoices: InvoiceRecordDto[] = [];
  private lastLoadedEnrolmentId: string | undefined;

  constructor(
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['enrolment'] || !this.enrolment) return;
    if (this.enrolment.id === this.lastLoadedEnrolmentId) return;
    this.lastLoadedEnrolmentId = this.enrolment.id;
    this.apiClient.byEnrolmentAll(this.enrolment.id).subscribe({
      next: (invoices) => { this.invoices = invoices; },
      error: () => {}
    });
  }

  get documents(): GenericDocument[] {
    return this.enrolment.documents;
  }

  get canManage(): boolean {
    return this.isOwner && this.permissions.edit;
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  downloadInvoice(invoice: InvoiceRecordDto): void {
    this.apiClient.downloadLink(invoice.id!).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  onFileSelected(file: File): void {
    this.isUploading = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.enrolmentService.addDocument(this.enrolment.id, {
          fileName: result.fileName ?? file.name,
          category: 'Other',
          url: result.url ?? '',
          contentType: file.type,
          sizeBytes: file.size
        }).subscribe({
          next: () => {
            this.isUploading = false;
            this.notificationService.success('Document uploaded.');
            this.changed.emit();
          },
          error: (err) => {
            this.isUploading = false;
            this.notificationService.error(extractHttpErrorMessage(err, 'Could not attach the uploaded file. Please try again.'));
          }
        });
      },
      error: () => {
        this.isUploading = false;
        this.notificationService.error('File upload failed. Please try again.');
      }
    });
  }

  onRename(event: { documentId: string; newFileName: string; note?: string }): void {
    this.enrolmentService.renameDocument(this.enrolment.id, event.documentId, event.newFileName, event.note).subscribe({
      next: () => this.changed.emit(),
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not rename this document.'));
      }
    });
  }

  onDelete(documentId: string): void {
    this.enrolmentService.deleteDocument(this.enrolment.id, documentId).subscribe({
      next: () => {
        this.notificationService.success('Document deleted.');
        this.changed.emit();
      },
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not delete this document.'));
      }
    });
  }
}
