import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { FileUploaderComponent, UploadCategory } from '@generic/file-uploader/file-uploader.component';

// Generic shape for one attached file — deliberately has nothing module-specific on it
// (no "category", no "courseApplicationId", etc.). A caller that needs extra scoping
// filters its own data down to this shape before passing it in as `files`.
export interface UploaderZoneFile {
  id: string;
  fileName: string;
  url: string;
  note?: string;
  uploadedByName?: string;
  uploadedAt?: string;
}

// Generic "attach files to something" zone — a label/count header, the list of already-
// attached files (download link + optional note input each), and a picker to add more.
// Knows nothing about any backend: it emits the raw File the user picked and the parent
// does the actual upload + persistence, then feeds the resulting list back in via
// `files`. Reusable by any module/step, not just Enrolments — see
// step-evidence-section.component.ts for how the Enrolment module wraps it.
@Component({
  selector: 'app-document-uploader-zone',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUploaderComponent],
  templateUrl: './document-uploader-zone.component.html',
  styleUrls: ['./document-uploader-zone.component.css']
})
export class DocumentUploaderZoneComponent {
  @Input({ required: true }) files: UploaderZoneFile[] = [];
  @Input({ required: true }) label!: string;
  @Input({ required: true }) emptyText!: string;
  @Input({ required: true }) uploadLabel!: string;
  @Input() maxCount?: number;
  @Input() canUpload = false;
  @Input() isUploading = false;
  // Per-file note input under each attached file. The caller owns persisting it (via
  // noteChanged) and reports which file(s) are mid-save via savingNoteFileIds.
  @Input() showNotes = false;
  @Input() savingNoteFileIds: string[] = [];
  // Passed straight through to app-file-uploader's own Settings-driven accept/size
  // limits — leave null for its plain defaults, or pass a category (e.g. 'enrolment').
  @Input() uploadCategory: UploadCategory | null = null;

  // Locked mode — the "file" is actually something completed elsewhere (e.g. an online
  // form response), so the picker is replaced with view/update actions on the first file.
  @Input() locked = false;
  @Input() lockedBadgeText = 'Completed online';
  @Input() lockedMessage = '';
  @Input() lockedFileLabel = '';
  @Input() lockedFileSub = '';
  @Output() viewLocked = new EventEmitter<void>();
  @Output() updateLocked = new EventEmitter<void>();

  @Output() fileSelected = new EventEmitter<File>();
  @Output() noteChanged = new EventEmitter<{ file: UploaderZoneFile; note: string }>();

  noteDraft: Record<string, string> = {};

  // Keeps the file rows (and the ngModel note inputs inside them) alive across change
  // detection even if a caller hands us a freshly-built array each pass — without this,
  // every pass destroys and recreates those inputs, which schedules another pass.
  trackByFileId(_index: number, file: UploaderZoneFile): string {
    return file.id;
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  noteValue(file: UploaderZoneFile): string {
    return this.noteDraft[file.id] ?? file.note ?? '';
  }

  isSavingNote(file: UploaderZoneFile): boolean {
    return this.savingNoteFileIds.includes(file.id);
  }

  onNoteBlur(file: UploaderZoneFile): void {
    const value = this.noteValue(file);
    this.noteDraft[file.id] = value;
    if (value === (file.note ?? '')) return;
    this.noteChanged.emit({ file, note: value });
  }
}
