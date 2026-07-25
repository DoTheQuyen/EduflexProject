import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

type PendingAction = 'close' | 'save' | 'update' | 'cancel' | 'delete' | null;

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

  pendingAction: PendingAction = null;

  get pendingMessage(): string {
    switch (this.pendingAction) {
      case 'close': return 'Are you sure you want to close this dialog?';
      case 'save': return `Are you sure you want to ${this.saveLabel.toLowerCase()}?`;
      case 'update': return `Are you sure you want to ${this.updateLabel.toLowerCase()}?`;
      case 'cancel': return `Are you sure you want to ${this.cancelLabel.toLowerCase()}?`;
      case 'delete': return `Are you sure you want to ${this.deleteLabel.toLowerCase()}?`;
      default: return '';
    }
  }

  onCloseClick(): void {
    if (!this.isSaving) {
      this.pendingAction = 'close';
    }
  }

  onSaveClick(): void {
    this.pendingAction = 'save';
  }

  onUpdateClick(): void {
    this.pendingAction = 'update';
  }

  onCancelClick(): void {
    this.pendingAction = 'cancel';
  }

  onDeleteClick(): void {
    this.pendingAction = 'delete';
  }

  confirmPendingAction(): void {
    switch (this.pendingAction) {
      case 'close': this.closeModal.emit(); break;
      case 'save': this.save.emit(); break;
      case 'update': this.update.emit(); break;
      case 'cancel': this.cancel.emit(); break;
      case 'delete': this.delete.emit(); break;
    }
    this.pendingAction = null;
  }

  cancelPendingAction(): void {
    this.pendingAction = null;
  }
}