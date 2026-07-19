import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DataTablesModule } from 'angular-datatables';
import { Feedback } from '../../../../models/feedback';
import { FeedbackService } from '../../../../services/feedback.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { formatDateTime } from '../../../../shared/utils/date-time.util';

@Component({
  selector: 'app-feedback-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTablesModule, DataTableComponent],
  templateUrl: './feedback-management.component.html',
  styleUrls: ['./feedback-management.component.css']
})
export class FeedbackManagementComponent implements OnInit {
  feedbacks: Feedback[] = [];
  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  photoPreview: string = '';

  feedbackForm: FormGroup;

  columns: DataTableColumn<Feedback>[] = [
    { field: 'photoUrl', title: 'Photo', className: 'text-center',
      render: (value) => `<img src="${value}" alt="" class="feedback-thumb">` },
    { field: 'name', title: 'Name' },
    { field: 'courseName', title: 'Course', className: 'text-center' },
    { field: 'comment', title: 'Comment', className: 'feedback-comment-cell' },
    { field: 'createdAt', title: 'Date', className: 'text-center',
      formatter: (value) => formatDateTime(value, 'mediumDate') },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

  rowActions: DataTableRowAction<Feedback>[] = [
    { action: 'delete', label: 'Delete', icon: 'fa-trash', cssClass: 'btn btn-sm btn-outline-danger' }
  ];

  constructor(private fb: FormBuilder, private feedbackService: FeedbackService) {
    this.feedbackForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      photoData: ['', [Validators.required]],
      photoContentType: ['image/jpeg'],
      courseName: ['', [Validators.required, Validators.maxLength(150)]],
      comment: ['', [Validators.required, Validators.maxLength(1000)]]
    });
  }

  ngOnInit(): void {
    this.loadFeedbacks();
  }

  loadFeedbacks(): void {
    this.isLoading = true;
    this.feedbackService.getAll().subscribe({
      next: (feedbacks) => {
        this.feedbacks = feedbacks;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.feedbackForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openModal(): void {
    this.feedbackForm.reset();
    this.photoPreview = '';
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    this.feedbackForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.feedbackForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    this.feedbackService.create(this.feedbackForm.value).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadFeedbacks();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error || 'Something went wrong saving the feedback. Please try again.';
      }
    });
  }

  onDelete(feedback: Feedback): void {
    const confirmed = window.confirm(`Delete feedback from ${feedback.name}?`);
    if (!confirmed) {
      return;
    }

    this.feedbackService.delete(feedback.id).subscribe({
      next: () => this.loadFeedbacks(),
      error: () => {
        window.alert('Could not delete this feedback. Please try again.');
      }
    });
  }

  onTableAction(event: DataTableAction<Feedback>): void {
    if (event.action === 'delete') {
      this.onDelete(event.row);
    }
  }

  onPhotoSelected(event: Event): void {
  const input = event.target as HTMLInputElement;
  const file = input.files && input.files[0];
  if (!file) { return; }

  const reader = new FileReader();
  reader.onload = () => {
    const img = new Image();
    img.onload = () => {
      const targetSize = 300;
      const canvas = document.createElement('canvas');
      canvas.width = targetSize;
      canvas.height = targetSize;
      const ctx = canvas.getContext('2d')!;

      const side = Math.min(img.width, img.height);
      const sx = (img.width - side) / 2;
      const sy = (img.height - side) / 2;
      ctx.drawImage(img, sx, sy, side, side, 0, 0, targetSize, targetSize);

      const dataUrl = canvas.toDataURL('image/jpeg', 0.8);
      const base64 = dataUrl.split(',')[1];

      this.photoPreview = dataUrl;
      this.feedbackForm.patchValue({
        photoData: base64,
        photoContentType: 'image/jpeg'
      });
      this.feedbackForm.get('photoData')?.markAsTouched();
    };
    img.src = reader.result as string;
  };
  reader.readAsDataURL(file);
  }
}