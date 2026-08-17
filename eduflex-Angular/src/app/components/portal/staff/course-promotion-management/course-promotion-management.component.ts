import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import {
  Client,
  CoursePromotionDto,
  CoursePromotionFilterDto,
  CreateCoursePromotionDto,
} from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { DataTableComponent } from '@generic/data-table/data-table.component';
import {
  DataTableColumn,
  DataTableAction,
  DataTableRowAction,
} from '@generic/data-table/data-table.models';
import { TablePagerState } from '@generic/data-table/table-pager-state';
import { formatDateTime } from '../../../../shared/utils/date-time.util';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';
import { NotificationService } from '@services/notification.service';
import { Button } from 'primeng/button';

const QUILL_TOOLBAR = [
  ['bold', 'italic', 'underline'],
  [{ list: 'ordered' }, { list: 'bullet' }],
  ['link'],
  ['clean'],
];

const MAX_OFFER_MONTHS = 12;

function maxExpiryDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) {
    return null;
  }
  const max = new Date();
  max.setMonth(max.getMonth() + MAX_OFFER_MONTHS);
  max.setHours(0, 0, 0, 0);
  const selected = new Date(control.value);
  return selected > max ? { maxExpiryDate: true } : null;
}

@Component({
  selector: 'app-course-promotion-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DataTableComponent, Button, QuillModule],
  templateUrl: './course-promotion-management.component.html',
  styleUrls: ['./course-promotion-management.component.css'],
})
export class CoursePromotionManagementComponent implements OnInit {
  readonly quillModules = { toolbar: QUILL_TOOLBAR };

  promotions: CoursePromotionDto[] = [];
  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  errorMessage = '';
  editingId: string | null = null;

  pager = new TablePagerState();
  isFeaturedFilter = 'all';

  permissions!: ModulePermissions;

  promotionForm: FormGroup;

  columns: DataTableColumn<CoursePromotionDto>[] = [
    {
      field: 'courseName',
      title: 'Course / University',
      render: (value, row) => `
        <div class="promo-course-name">${value}</div>
        <div class="promo-uni-name">${row.universityName}</div>
      `,
    },
    { field: 'semester', title: 'Semester' },
    {
      field: 'scholarshipLabel',
      title: 'Scholarship',
      render: (value) => `<span class="badge bg-warning text-dark">${value}</span>`,
    },
    { field: 'location', title: 'Location', hideOnTablet: true },
    { field: 'tuition', title: 'Tuition' },
    {
      field: 'expiryDate',
      title: 'Offer ends',
      className: 'text-center',
      formatter: (value) => formatDateTime(value, 'mediumDate'),
      hideOnLaptop: true,
    },
    {
      field: 'isFeatured',
      title: 'Featured',
      className: 'text-center',
      render: (value) =>
        value === 'Yes'
          ? `<span class="badge bg-success">Yes</span>`
          : `<span class="badge bg-secondary">No</span>`,
      hideOnLaptop: true,
    },
    { field: 'actions', title: 'Actions', className: 'text-center' },
  ];

  rowActions: DataTableRowAction<CoursePromotionDto>[] = [];

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
  ) {
    this.promotionForm = this.fb.group({
      courseName: ['', [Validators.required, Validators.maxLength(150)]],
      universityName: ['', [Validators.required, Validators.maxLength(150)]],
      semester: ['', [Validators.required, Validators.maxLength(50)]],
      scholarshipLabel: ['', [Validators.required, Validators.maxLength(80)]],
      location: ['', [Validators.required, Validators.maxLength(100)]],
      tuition: ['', [Validators.required, Validators.maxLength(80)]],
      opportunities: ['', [Validators.required, Validators.maxLength(150)]],
      expiryDate: ['', [Validators.required, maxExpiryDateValidator]],
      note: ['', [Validators.maxLength(5000)]],
      websiteUrl: [''],
      isFeatured: [true],
      displayOrder: [0, [Validators.required, Validators.min(0)]],
    });

    this.permissions = this.authHelper.hasCoursePromotionsPermission();

    this.rowActions = [
      ...(this.permissions.edit
        ? [
            {
              action: 'edit',
              label: 'Edit',
              icon: 'fa-edit',
              cssClass: 'btn btn-sm btn-outline-primary',
            },
          ]
        : []),
      ...(this.permissions.delete
        ? [
            {
              action: 'delete',
              label: 'Delete',
              icon: 'fa-trash',
              cssClass: 'btn btn-sm btn-outline-danger',
            },
          ]
        : []),
    ];
  }

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view course promotions.');
      return;
    }

    this.isLoading = true;
    const isFeatured =
      this.isFeaturedFilter === 'all' ? undefined : this.isFeaturedFilter === 'true';
    const filter = new CoursePromotionFilterDto({
      pageNumber: this.pager.pageNumber,
      pageSize: this.pager.pageSize,
      searchTerm: this.pager.searchTerm || undefined,
      isFeatured,
    });
    this.apiClient.searchCoursePromotions(filter).subscribe({
      next: (result) => {
        this.promotions = result.items ?? [];
        this.pager.totalCount = result.totalCount ?? 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
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

  onRefresh(): void {
    this.isFeaturedFilter = 'all';
    this.pager.search('');
    this.loadPromotions();
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.promotionForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  get maxExpiryDate(): string {
    const d = new Date();
    d.setMonth(d.getMonth() + MAX_OFFER_MONTHS);
    return d.toISOString().substring(0, 10);
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
      displayOrder: promotion.displayOrder,
    });
    this.errorMessage = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubmit(): void {
    const requiredPermission = this.editingId ? this.permissions.edit : this.permissions.add;
    if (!requiredPermission) {
      this.notificationService.error(
        this.editingId
          ? 'You do not have permission to edit course promotions.'
          : 'You do not have permission to add course promotions.',
      );
      return;
    }

    this.promotionForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.promotionForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const value = this.promotionForm.value;
    const payload = new CreateCoursePromotionDto({
      ...value,
      expiryDate: new Date(value.expiryDate),
    });

    const request$ = this.editingId
      ? this.apiClient.coursePromotionsPUT(this.editingId, payload)
      : this.apiClient.coursePromotionsPOST(payload);

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.loadPromotions();
        this.notificationService.success(
          this.editingId
            ? 'Course promotion updated successfully.'
            : 'Course promotion created successfully.',
        );
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(
          err,
          'Something went wrong saving the course promotion. Please try again.',
        );
        this.notificationService.error(this.errorMessage);
      },
    });
  }

  onDelete(promotion: CoursePromotionDto): void {
    if (!this.permissions.delete) {
      this.notificationService.error('You do not have permission to delete course promotions.');
      return;
    }

    const confirmed = window.confirm(
      `Delete the promotion for ${promotion.courseName} (${promotion.universityName})?`,
    );
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
      },
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
