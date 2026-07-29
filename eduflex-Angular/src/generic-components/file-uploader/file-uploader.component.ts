import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsService } from '@services/settings.service';
import { SettingsDto, UploadLimitDto } from '@services/api.services';

export type UploadCategory = 'image' | 'contract' | 'enrolment';

const CATEGORY_FALLBACKS: Record<UploadCategory, { accept: string; maxSizeMB: number }> = {
  image: { accept: 'image/*', maxSizeMB: 2 },
  contract: { accept: '.pdf,.doc,.docx', maxSizeMB: 10 },
  enrolment: { accept: '*', maxSizeMB: 10 }
};

@Component({
  selector: 'app-file-uploader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-uploader.component.html',
  styleUrls: ['./file-uploader.component.css']
})
export class FileUploaderComponent implements OnInit {
  @Input() variant: 'avatar' | 'dropzone' = 'dropzone';
  @Input() label = 'Upload a file';
  @Input() accept = 'image/*';
  @Input() maxSizeMB = 5;
  // When set, this overrides accept/maxSizeMB above once the DB-backed Settings module
  // resolves — one place to know how uploaders get their limits, instead of every call
  // site fetching Client.settingsGET() and computing its own fallback.
  @Input() uploadCategory: UploadCategory | null = null;
  @Input() previewUrl: string | null = null;

  @Output() fileSelected = new EventEmitter<File>();
  @Output() invalidFile = new EventEmitter<string>();

  isDragging = false;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    if (!this.uploadCategory) {
      return;
    }
    const fallback = CATEGORY_FALLBACKS[this.uploadCategory];
    this.accept = fallback.accept;
    this.maxSizeMB = fallback.maxSizeMB;

    this.settingsService.getSettings().subscribe({
      next: (settings) => {
        const limit = this.limitForCategory(settings);
        if (!limit) {
          return;
        }
        this.accept = limit.allowedExtensions?.length ? limit.allowedExtensions.join(',') : '*';
        this.maxSizeMB = limit.maxSizeMB ?? this.maxSizeMB;
      },
      error: () => {}
    });
  }

  private limitForCategory(settings: SettingsDto): UploadLimitDto | undefined {
    switch (this.uploadCategory) {
      case 'image': return settings.imageUpload;
      case 'contract': return settings.contractUpload;
      case 'enrolment': return settings.enrolmentUpload;
      default: return undefined;
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.handleFile(file);
    }
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.handleFile(file);
    }
    input.value = '';
  }

  private handleFile(file: File): void {
    if (this.accept && this.accept !== '*' && !this.matchesAccept(file)) {
      this.invalidFile.emit(`"${file.name}" is not an accepted file type.`);
      return;
    }
    const maxBytes = this.maxSizeMB * 1024 * 1024;
    if (file.size > maxBytes) {
      this.invalidFile.emit(`"${file.name}" is larger than ${this.maxSizeMB}MB.`);
      return;
    }
    this.fileSelected.emit(file);
  }

  private matchesAccept(file: File): boolean {
    const patterns = this.accept.split(',').map(p => p.trim()).filter(Boolean);
    const fileExtension = file.name.includes('.')
      ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
      : '';

    return patterns.some(pattern => {
      if (pattern.startsWith('.')) {
        return pattern.toLowerCase() === fileExtension;
      }
      if (pattern.endsWith('/*')) {
        return file.type.startsWith(pattern.replace('/*', '/'));
      }
      return file.type === pattern;
    });
  }
}
