import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client, InvoiceRecordDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EnrolmentDocument, DocumentCategory, DOCUMENT_CATEGORY_LABELS, documentCategoryLabel } from '../../../../../../../models/enrolment';

const DOCUMENT_STEP_CATEGORIES: DocumentCategory[] = ['GS', 'UniOffer', 'CoE', 'VisaDraft', 'VisaGranted'];

@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent],
  templateUrl: './documents-tab.component.html',
  styleUrls: ['./documents-tab.component.css']
})
export class DocumentsTabComponent implements OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Output() changed = new EventEmitter<void>();

  readonly documentStepCategories = DOCUMENT_STEP_CATEGORIES;
  readonly documentCategoryLabels = DOCUMENT_CATEGORY_LABELS;

  isUploading = false;
  renamingDocumentId: string | null = null;
  renameValue = '';

  // Invoices sent to the student for this enrolment — shown alongside "Other documents"
  // (in the grid slot next to VISA Granted) since staff think of "everything sent to or
  // collected from this student" as one list, not two separate places to check.
  invoices: InvoiceRecordDto[] = [];
  private lastLoadedEnrolmentId: string | undefined;

  constructor(
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['enrolment'] || !this.enrolment || this.enrolment.id === this.lastLoadedEnrolmentId) return;
    this.lastLoadedEnrolmentId = this.enrolment.id;
    this.apiClient.byEnrolmentAll(this.enrolment.id).subscribe({
      next: (invoices) => { this.invoices = invoices; },
      error: () => {}
    });
  }

  downloadInvoice(invoice: InvoiceRecordDto): void {
    this.apiClient.downloadLink2(invoice.id!).subscribe({
      next: (result) => { window.open(result.url, '_blank', 'noopener'); },
      error: () => { this.notificationService.error('Could not resolve the download link.'); }
    });
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  documentsByCategory(category: DocumentCategory): EnrolmentDocument[] {
    return this.enrolment.documents.filter(d => d.category === category);
  }

  get otherDocuments(): EnrolmentDocument[] {
    return this.enrolment.documents.filter(d => !d.category || d.category === 'Other');
  }

  categoryLabel(category?: string): string {
    return documentCategoryLabel(category);
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

  startRename(doc: EnrolmentDocument): void {
    this.renamingDocumentId = doc.id;
    this.renameValue = doc.fileName;
  }

  confirmRename(doc: EnrolmentDocument): void {
    if (!this.renameValue.trim()) return;
    this.enrolmentService.renameDocument(this.enrolment.id, doc.id, this.renameValue.trim()).subscribe({
      next: () => {
        this.renamingDocumentId = null;
        this.changed.emit();
      },
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not rename this document.'));
      }
    });
  }

  cancelRename(): void {
    this.renamingDocumentId = null;
  }

  deleteDocument(doc: EnrolmentDocument): void {
    if (!window.confirm(`Delete "${doc.fileName}"?`)) return;
    this.enrolmentService.deleteDocument(this.enrolment.id, doc.id).subscribe({
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
