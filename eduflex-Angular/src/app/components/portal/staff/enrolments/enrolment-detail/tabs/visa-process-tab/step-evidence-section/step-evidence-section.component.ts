import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Client } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { DocumentUploaderZoneComponent, UploaderZoneFile } from '@generic/document-uploader-zone/document-uploader-zone.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { Enrolment, EnrolmentDocument, DocumentCategory } from '../../../../../../../../models/enrolment';

// Enrolment-specific adapter around the generic app-document-uploader-zone — maps
// EnrolmentDocument[] (category/courseApplicationId-scoped) to the generic
// UploaderZoneFile shape, and wires uploads/notes to EnrolmentService. Used 5+ times by
// visa-process-tab.component.html and course-applications-panel.component.html, so the
// Enrolment<->API wiring lives in one place instead of being repeated at each call site.
// isOwner/permissions are kept as inputs purely so existing call sites don't need to
// change their bindings — canUpload (computed by the caller from those same two things)
// is what actually gates the UI now, both here and inside the generic component.
@Component({
  selector: 'app-step-evidence-section',
  standalone: true,
  imports: [CommonModule, DocumentUploaderZoneComponent],
  templateUrl: './step-evidence-section.component.html',
  styleUrls: ['./step-evidence-section.component.css']
})
export class StepEvidenceSectionComponent implements OnChanges {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input({ required: true }) category!: DocumentCategory;
  @Input({ required: true }) label!: string;
  @Input({ required: true }) emptyText!: string;
  @Input({ required: true }) uploadLabel!: string;
  @Input() maxCount?: number;
  @Input() canUpload = false;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Input() courseApplicationId?: string;
  @Input() showNotes = false;

  @Input() locked = false;
  @Input() lockedBadgeText = 'Completed online';
  @Input() lockedMessage = '';
  @Input() lockedFileLabel = '';
  @Input() lockedFileSub = '';
  @Output() viewLocked = new EventEmitter<void>();
  @Output() updateLocked = new EventEmitter<void>();

  @Output() changed = new EventEmitter<void>();

  isUploading = false;
  savingNoteFileIds: string[] = [];

  // Computed once per input change rather than from a template-bound getter. A getter
  // here would hand the child a brand-new array (with brand-new object literals) on
  // every change-detection pass, which makes its *ngFor tear down and rebuild every row
  // — including the ngModel note inputs — and that in turn schedules another pass, so
  // change detection never settles. Recomputing only in ngOnChanges keeps the reference
  // stable between passes.
  files: UploaderZoneFile[] = [];

  constructor(
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private notificationService: NotificationService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['enrolment'] || changes['category'] || changes['courseApplicationId']) {
      this.files = this.documents.map(d => ({
        id: d.id, fileName: d.fileName, url: d.url, note: d.note,
        uploadedByName: d.uploadedByName, uploadedAt: d.uploadedAt
      }));
    }
  }

  get documents(): EnrolmentDocument[] {
    return this.enrolment.documents.filter(d =>
      d.category === this.category && (this.courseApplicationId === undefined || d.courseApplicationId === this.courseApplicationId));
  }

  onNoteChanged(event: { file: UploaderZoneFile; note: string }): void {
    this.savingNoteFileIds = [...this.savingNoteFileIds, event.file.id];
    this.enrolmentService.renameDocument(this.enrolment.id, event.file.id, event.file.fileName, event.note || undefined).subscribe({
      next: () => {
        this.savingNoteFileIds = this.savingNoteFileIds.filter(id => id !== event.file.id);
        this.changed.emit();
      },
      error: (err) => {
        this.savingNoteFileIds = this.savingNoteFileIds.filter(id => id !== event.file.id);
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this note.'));
      }
    });
  }

  onFileSelected(file: File): void {
    this.isUploading = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.enrolmentService.addDocument(this.enrolment.id, {
          fileName: result.fileName ?? file.name,
          category: this.category,
          courseApplicationId: this.courseApplicationId,
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
            this.notificationService.error(extractHttpErrorMessage(err, 'Could not attach the uploaded file.'));
          }
        });
      },
      error: () => {
        this.isUploading = false;
        this.notificationService.error('File upload failed. Please try again.');
      }
    });
  }
}
