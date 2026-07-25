import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, CoursePromotionDto, CoursePromotionFilterDto, CreateCoursePromotionDto } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import { DataTableColumn, DataTableAction, DataTableRowAction } from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { formatDateTime } from '../../../../shared/utils/date-time.util';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';
import { PermissionKeys } from '../../../../shared/constants/permission-keys';
import { NotificationService } from '@services/notification.service';

@Component({
  selector: 'app-course-promotion-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTableComponent],
  templateUrl: './course-promotion-management.component.html',
  styleUrls: ['./course-promotion-management.component.css']
})
export class CoursePromotionManagementComponent implements OnInit {
  promotions: CoursePromotionDto[] = [];
  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();
  isFeaturedFilter = 'all';

  canAdd = false;
  canEdit = false;
  canDelete = false;

  promotionForm: FormGroup;

  columns: DataTableColumn<CoursePromotionDto>[] = [
    { field: 'courseName', title: 'Course / University',
      render: (value, row) => `
        <div class="promo-course-name">${value}</div>
        <div class="promo-uni-name">${row.universityName}</div>
      ` },
    { field: 'semester', title: 'Semester' },
    { field: 'scholarshipLabel', title: 'Scholarship',
      render: (value) => `<span class="badge bg-warning text-dark">${value}</span>` },
    { field: 'location', title: 'Location' },
    { field: 'tuition', title: 'Tuition' },
    { field: 'expiryDate', title: 'Offer ends', className: 'text-center',
      formatter: (value) => formatDateTime(value, 'mediumDate') },
    { field: 'isFeatured', title: 'Featured', className: 'text-center',
      render: (value) => value === 'Yes'
        ? `<span class="badge bg-success">Yes</span>`
        : `<span class="badge bg-secondary">No</span>` },
    { field: 'actions', title: 'Actions', className: 'text-center' }
  ];

  rowActions: DataTableRowAction<CoursePromotionDto>[] = [];

  constructor(private fb: FormBuilder, private apiClient: Client, private authHelper: AuthHelperService, private notificationService: NotificationService) {
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

    this.canAdd = this.authHelper.hasPermission(PermissionKeys.CoursePromotionsAdd);
    this.canEdit = this.authHelper.hasPermission(PermissionKeys.CoursePromotionsEdit);
    this.canDelete = this.authHelper.hasPermission(PermissionKeys.CoursePromotionsDelete);

    this.rowActions = [
      ...(this.canEdit ? [{ action: 'edit', label: 'Edit', icon: 'fa-edit', cssClass: 'btn btn-sm btn-outline-primary' }] : []),
      ...(this.canDelete ? [{ action: 'delete', label: 'Delete', icon: 'fa-trash', cssClass: 'btn btn-sm btn-outline-danger' }] : [])
    ];
  }

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    this.isLoading = true;
    const isFeatured = this.isFeaturedFilter === 'all' ? undefined : this.isFeaturedFilter === 'true';
    const filter = new CoursePromotionFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      isFeatured
    });
    this.apiClient.searchCoursePromotions(filter).subscribe({
      next: (result) => {
        this.promotions = result.items ?? [];
        this.pager.totalCount = result.totalCount ?? 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.pager.goToPage(page);
    this.loadPromotions();
  }

  onSearchChange(term: string): void {
    this.pager.search(term);
    this.loadPromotions();
  }

  onFeaturedFilterChange(event: Event): void {
    this.isFeaturedFilter = (event.target as HTMLSelectElement).value;
    this.pager.goToPage(1);
    this.loadPromotions();
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

  openEditModal(promotion: CoursePromotionDto): void {
    this.editingId = promotion.id ?? null;
    this.promotionForm.reset({
      courseName: promotion.courseName,
      universityName: promotion.universityName,
      semester: promotion.semester,
      scholarshipLabel: promotion.scholarshipLabel,
      location: promotion.location,
      tuition: promotion.tuition,
      opportunities: promotion.opportunities,
      expiryDate: promotion.expiryDate ? promotion.expiryDate.toISOString().substring(0, 10) : '',
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
    const payload = new CreateCoursePromotionDto({
      ...value,
      expiryDate: new Date(value.expiryDate)
    });

    const request$ = this.editingId
      ? this.apiClient.coursePromotionsPUT(this.editingId, payload)
      : this.apiClient.coursePromotionsPOST(payload);

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadPromotions();
        this.notificationService.success(this.editingId ? 'Course promotion updated successfully.' : 'Course promotion created successfully.');
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the course promotion. Please try again.');
        this.notificationService.error(this.errorMessage);
      }
    });
  }

  onDelete(promotion: CoursePromotionDto): void {
    const confirmed = window.confirm(`Delete the promotion for ${promotion.courseName} (${promotion.universityName})?`);
    if (!confirmed || !promotion.id) {
      return;
    }

    this.apiClient.coursePromotionsDELETE(promotion.id).subscribe({
      next: () => {
        this.loadPromotions();
        this.notificationService.success('Course promotion deleted successfully.');
      },
      error: () => {
        this.notificationService.error('Could not delete this course promotion. Please try again.');
      }
    });
  }

  onTableAction(event: DataTableAction<CoursePromotionDto>): void {
    if (event.action === 'edit') {
      this.openEditModal(event.row);
    } else if (event.action === 'delete') {
      this.onDelete(event.row);
    }
  }
}
