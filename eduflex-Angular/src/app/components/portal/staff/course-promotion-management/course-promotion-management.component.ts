import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CoursePromotion } from '../../../../models/course-promotion';
import { CoursePromotionService } from '../../../../services/course-promotion.service';

@Component({
  selector: 'app-course-promotion-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './course-promotion-management.component.html',
  styleUrls: ['./course-promotion-management.component.css']
})
export class CoursePromotionManagementComponent implements OnInit {
  promotions: CoursePromotion[] = [];
  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  promotionForm: FormGroup;

  constructor(private fb: FormBuilder, private coursePromotionService: CoursePromotionService) {
    this.promotionForm = this.fb.group({
      courseName: ['', [Validators.required, Validators.maxLength(150)]],
      universityName: ['', [Validators.required, Validators.maxLength(150)]],
      semester: ['', [Validators.required, Validators.maxLength(50)]],
      scholarshipLabel: ['', [Validators.required, Validators.maxLength(80)]],
      location: ['', [Validators.required, Validators.maxLength(100)]],
      tuition: ['', [Validators.required, Validators.maxLength(80)]],
      opportunities: ['', [Validators.required, Validators.maxLength(150)]],
      expiryDate: ['', [Validators.required]],
      note: ['', [Validators.maxLength(600)]],
      websiteUrl: ['', [Validators.required]],
      isFeatured: [true],
      displayOrder: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    this.isLoading = true;
    this.coursePromotionService.getAll().subscribe({
      next: (promotions) => {
        this.promotions = promotions;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.promotionForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openAddModal(): void {
    this.editingId = null;
    this.promotionForm.reset({ isFeatured: true, displayOrder: 0 });
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  openEditModal(promotion: CoursePromotion): void {
    this.editingId = promotion.id;
    this.promotionForm.reset({
      courseName: promotion.courseName,
      universityName: promotion.universityName,
      semester: promotion.semester,
      scholarshipLabel: promotion.scholarshipLabel,
      location: promotion.location,
      tuition: promotion.tuition,
      opportunities: promotion.opportunities,
      expiryDate: promotion.expiryDate ? promotion.expiryDate.substring(0, 10) : '',
      note: promotion.note,
      websiteUrl: promotion.websiteUrl,
      isFeatured: promotion.isFeatured,
      displayOrder: promotion.displayOrder
    });
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    this.promotionForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.promotionForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const value = this.promotionForm.value;
    const payload = {
      ...value,
      expiryDate: new Date(value.expiryDate).toISOString()
    };

    const request$ = this.editingId
      ? this.coursePromotionService.update(this.editingId, payload)
      : this.coursePromotionService.create(payload);

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadPromotions();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error || 'Something went wrong saving the course promotion. Please try again.';
      }
    });
  }

  onDelete(promotion: CoursePromotion): void {
    const confirmed = window.confirm(`Delete the promotion for ${promotion.courseName} (${promotion.universityName})?`);
    if (!confirmed) {
      return;
    }

    this.coursePromotionService.delete(promotion.id).subscribe({
      next: () => this.loadPromotions(),
      error: () => {
        window.alert('Could not delete this course promotion. Please try again.');
      }
    });
  }
}
