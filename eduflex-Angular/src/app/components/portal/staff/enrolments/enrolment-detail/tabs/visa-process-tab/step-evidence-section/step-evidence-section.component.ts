import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Client } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EnrolmentDocument, DocumentCategory } from '../../../../../../../../models/enrolment';

// One step's evidence file slot (GS/UniOffer/CoE/VisaDraft/VisaGranted) — used 5 times
// by visa-process-tab.component.html, extracted so that repeated markup/CSS lives in
// one place instead of five near-identical copies. Also supports a "locked" mode for
// the one case (GS) where an online form response replaces manual upload entirely.
@Component({
  selector: 'app-step-evidence-section',
  standalone: true,
  imports: [CommonModule, FileUploaderComponent],
  templateUrl: './step-evidence-section.component.html',
  styleUrls: ['./step-evidence-section.component.css']
})
export class StepEvidenceSectionComponent {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input({ required: true }) category!: DocumentCategory;
  @Input({ required: true }) label!: string;
  @Input({ required: true }) emptyText!: string;
  @Input({ required: true }) uploadLabel!: string;
  @Input() maxCount?: number;
  @Input() canUpload = false;
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;

  // Locked mode — the evidence "file" is actually a form response completed online
  // (currently only the GS statement), so upload is replaced with view/update actions.
  @Input() locked = false;
  @Input() lockedBadgeText = 'Completed online';
  @Input() lockedMessage = '';
  @Input() lockedFileLabel = '';
  @Input() lockedFileSub = '';
  @Output() viewLocked = new EventEmitter<void>();
  @Output() updateLocked = new EventEmitter<void>();

  @Output() changed = new EventEmitter<void>();

  isUploading = false;

  constructor(
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private notificationService: NotificationService
  ) {}

  get documents(): EnrolmentDocument[] {
    return this.enrolment.documents.filter(d => d.category === this.category);
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  onFileSelected(file: File): void {
    this.isUploading = true;
    this.apiClient.upload({ data: file, fileName: file.name }).subscribe({
      next: (result) => {
        this.enrolmentService.addDocument(this.enrolment.id, {
          fileName: result.fileName ?? file.name,
          category: this.category,
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
