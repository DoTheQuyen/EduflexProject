import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { DocumentUploaderZoneComponent, UploaderZoneFile } from '@generic/document-uploader-zone/document-uploader-zone.component';

export interface GenericDocument {
  id: string;
  fileName: string;
  category?: string | null;
  note?: string | null;
  url: string;
  uploadedByName: string;
  uploadedAt: string;
  isFromStudent?: boolean;
}

export interface DocumentCategoryDef {
  key: string;
  label: string;
}

// Fully generic "documents" tab — no knowledge of Enrolment, MigrationCase, invoices, or
// any other module-specific concept. A caller supplies which categories to show as
// dedicated (read-only-here) zones plus its own document list; anything whose category
// isn't in that list falls into "Other documents", where uploading is allowed. The
// category zones stay read-only here on purpose, matching Enrolment's existing behaviour
// — uploading to a specific category happens through that category's own step/evidence
// zone elsewhere (visa-process-tab for Enrolment, the Process tab for Migration Case), not
// from this rollup view. Upload/rename/delete are all just emitted events; the caller owns
// the actual API calls, so this component has zero backend coupling.
@Component({
  selector: 'app-generic-documents-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent, DocumentUploaderZoneComponent],
  templateUrl: './generic-documents-tab.component.html',
  styleUrls: ['./generic-documents-tab.component.css']
})
export class GenericDocumentsTabComponent implements OnChanges {
  @Input({ required: true }) documents: GenericDocument[] = [];
  @Input({ required: true }) categories: DocumentCategoryDef[] = [];
  @Input() canManage = false;
  @Input() isUploading = false;
  @Input() introText = '';
  @Input() otherLabel = 'Other documents';

  @Output() fileSelected = new EventEmitter<File>();
  @Output() rename = new EventEmitter<{ documentId: string; newFileName: string; note?: string }>();
  @Output() deleteDocument = new EventEmitter<string>();

  renamingDocumentId: string | null = null;
  renameValue = '';

  // Same reasoning as documents-tab.component.ts's filesByCategoryMap — precomputed on
  // input change so *ngFor/ngModel rows stay stable across change-detection passes.
  filesByCategoryMap: Record<string, UploaderZoneFile[]> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['documents'] && !changes['categories']) return;

    this.filesByCategoryMap = {};
    for (const category of this.categories) {
      this.filesByCategoryMap[category.key] = this.documentsByCategory(category.key).map((d) => ({
        id: d.id, fileName: d.fileName, url: d.url, note: d.note ?? undefined,
        uploadedByName: d.uploadedByName, uploadedAt: d.uploadedAt
      }));
    }
  }

  documentsByCategory(categoryKey: string): GenericDocument[] {
    return this.documents.filter((d) => d.category === categoryKey);
  }

  get otherDocuments(): GenericDocument[] {
    const categoryKeys = this.categories.map((c) => c.key);
    return this.documents.filter((d) => !d.category || !categoryKeys.includes(d.category));
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  onFileSelected(file: File): void {
    this.fileSelected.emit(file);
  }

  startRename(doc: GenericDocument): void {
    this.renamingDocumentId = doc.id;
    this.renameValue = doc.fileName;
  }

  confirmRename(doc: GenericDocument): void {
    if (!this.renameValue.trim()) return;
    this.rename.emit({ documentId: doc.id, newFileName: this.renameValue.trim(), note: doc.note ?? undefined });
    this.renamingDocumentId = null;
  }

  cancelRename(): void {
    this.renamingDocumentId = null;
  }

  onDelete(doc: GenericDocument): void {
    if (!window.confirm(`Delete "${doc.fileName}"?`)) return;
    this.deleteDocument.emit(doc.id);
  }
}
