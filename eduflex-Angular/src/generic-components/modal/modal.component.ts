import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.css']
})
export class ModalComponent {
  @Input() title = '';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() isSaving = false;
  @Input() saveDisabled = false;

  @Input() showSave = true;
  @Input() showUpdate = false;
  @Input() showCancel = true;
  @Input() showDelete = false;

  @Input() saveLabel = 'Save';
  @Input() updateLabel = 'Update';
  @Input() cancelLabel = 'Cancel';
  @Input() deleteLabel = 'Delete';
  @Input() savingLabel = 'Saving...';

  @Output() closeModal = new EventEmitter<void>();
  @Output() save = new EventEmitter<void>();
  @Output() update = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() delete = new EventEmitter<void>();

  onBackdropClick(): void {
    if (!this.isSaving) {
      this.closeModal.emit();
    }
  }

  onCloseClick(): void {
    if (!this.isSaving) {
      this.closeModal.emit();
    }
  }
}
