import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, Dialog, Button],
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

  constructor(private confirmationService: ConfirmationService) {}

  get dialogWidth(): string {
    switch (this.size) {
      case 'sm': return '30vw';
      case 'lg': return '70vw';
      default: return '50vw';
    }
  }

  onCloseClick(): void {
    if (this.isSaving) return;
    this.confirmationService.confirm({
      message: 'Are you sure you want to close this dialog?',
      accept: () => this.closeModal.emit(),
    });
  }

  onSaveClick(): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to ${this.saveLabel.toLowerCase()}?`,
      accept: () => this.save.emit(),
    });
  }

  onUpdateClick(): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to ${this.updateLabel.toLowerCase()}?`,
      accept: () => this.update.emit(),
    });
  }

  onCancelClick(): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to ${this.cancelLabel.toLowerCase()}?`,
      accept: () => this.cancel.emit(),
    });
  }

  onDeleteClick(): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to ${this.deleteLabel.toLowerCase()}?`,
      accept: () => this.delete.emit(),
    });
  }
}
