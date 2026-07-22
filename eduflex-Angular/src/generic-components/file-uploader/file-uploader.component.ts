import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-uploader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-uploader.component.html',
  styleUrls: ['./file-uploader.component.css']
})
export class FileUploaderComponent {
  @Input() variant: 'avatar' | 'dropzone' = 'dropzone';
  @Input() label = 'Upload a file';
  @Input() accept = 'image/*';
  @Input() maxSizeMB = 5;
  @Input() previewUrl: string | null = null;

  @Output() fileSelected = new EventEmitter<File>();
  @Output() invalidFile = new EventEmitter<string>();

  isDragging = false;

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
    const patterns = this.accept.split(',').map(p => p.trim());
    return patterns.some(pattern => {
      if (pattern.endsWith('/*')) {
        return file.type.startsWith(pattern.replace('/*', '/'));
      }
      return file.type === pattern;
    });
  }
}