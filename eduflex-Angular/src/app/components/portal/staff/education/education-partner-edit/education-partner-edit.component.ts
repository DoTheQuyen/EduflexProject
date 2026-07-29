import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Client, EducationPartnerDto, CreateEducationPartnerDto, CourseDto, CreateCourseDto, BusinessPartnerDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

const PARTNER_TYPES = ['University', 'College/Education Organization'];
const CURRENCIES = ['AUD', 'USD', 'GBP', 'EUR', 'NZD', 'SGD', 'CAD'];

@Component({
  selector: 'app-education-partner-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, FileUploaderComponent],
  templateUrl: './education-partner-edit.component.html',
  styleUrls: ['./education-partner-edit.component.css']
})
export class EducationPartnerEditComponent implements OnInit {
  partnerId: string | null = null;
  activeTab: 'info' | 'course' = 'info';
  mode: 'view' | 'edit' = 'edit';
  partnerTypes = PARTNER_TYPES;
  currencies = CURRENCIES;
  permissions!: ModulePermissions;

  private partnerSnapshot: EducationPartnerDto | null = null;

  isLoading = false;
  isSaving = false;
  isUploadingLogo = false;
  errorMessage = '';
  logoPreviewUrl: string | null = null;
  private pendingLogoFile: File | null = null;

  get logoMissing(): boolean {
    return !this.pendingLogoFile && !this.partnerForm.value.logoUrl;
  }

  businessPartners: BusinessPartnerDto[] = [];
  businessPartnerName: string | null = null;

  courses: CourseDto[] = [];
  editingCourseId: string | null = null;
  isCourseFormOpen = false;
  isSavingCourse = false;
  courseErrorMessage = '';

  partnerForm: FormGroup;
  courseForm: FormGroup;

  get intakesArray(): FormArray {
    return this.partnerForm.get('intakes') as FormArray;
  }

  get isCourseTabEnabled(): boolean {
    return !!this.partnerId;
  }

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private route: ActivatedRoute,
    private router: Router,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasEducationPartnersPermission();

    this.partnerForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      trademark: ['', [Validators.required, Validators.maxLength(100)]],
      country: ['', [Validators.required, Validators.maxLength(100)]],
      partnerType: ['University', [Validators.required]],
      location: ['', [Validators.required, Validators.maxLength(150)]],
      email: ['', [Validators.email, Validators.maxLength(150)]],
      phoneNumber: ['', [Validators.maxLength(30)]],
      description: ['', [Validators.required, Validators.maxLength(300)]],
      logoUrl: [''],
      intakes: this.fb.array([]),
      businessPartnerId: [null as string | null],
      commissionBaseRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      abn: ['', [Validators.pattern(/^\d{11}$/)]],
      acn: ['', [Validators.pattern(/^\d{9}$/)]]
    });

    this.courseForm = this.fb.group({
      courseName: ['', [Validators.required, Validators.maxLength(150)]],
      intakes: [[] as string[]],
      studyModes: [[] as string[]],
      campuses: [[] as string[]],
      tuitionFee: [0, [Validators.required, Validators.min(1)]],
      totalTuitionFee: [0, [Validators.required, Validators.min(1)]],
      tuitionCurrency: ['AUD', [Validators.required]],
      courseDurationMonths: [null as number | null],
      commissionBaseRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const tab = this.route.snapshot.queryParamMap.get('tab');

    this.partnerId = id;
    this.activeTab = tab === 'course' && id ? 'course' : 'info';
    this.mode = id ? 'view' : 'edit';

    this.loadBusinessPartners();

    if (id) {
      this.loadPartner(id);
    } else {
      this.applyFormMode();
    }
  }

  private loadBusinessPartners(): void {
    this.apiClient.businessPartnersAll().subscribe({
      next: (partners) => {
        this.businessPartners = partners ?? [];
        this.updateBusinessPartnerName();
      },
      error: () => {}
    });
  }

  private updateBusinessPartnerName(): void {
    const id = this.partnerForm.value.businessPartnerId;
    this.businessPartnerName = id ? (this.businessPartners.find(bp => bp.id === id)?.name ?? null) : null;
  }

  onBusinessPartnerChange(): void {
    this.updateBusinessPartnerName();
  }

  switchTab(tab: 'info' | 'course'): void {
    if (tab === 'course' && !this.isCourseTabEnabled) {
      return;
    }
    this.activeTab = tab;
  }

  loadPartner(id: string): void {
    this.isLoading = true;
    this.apiClient.educationPartnersGET(id).subscribe({
      next: (partner) => {
        this.partnerSnapshot = partner;
        this.applyPartnerToForm(partner);
        this.applyFormMode();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this education partner.');
      }
    });
  }

  private applyPartnerToForm(partner: EducationPartnerDto): void {
    this.partnerForm.patchValue({
      name: partner.name,
      trademark: partner.trademark,
      country: partner.country,
      partnerType: partner.partnerType,
      location: partner.location,
      email: partner.email,
      phoneNumber: partner.phoneNumber,
      description: partner.description,
      logoUrl: partner.logoUrl,
      businessPartnerId: partner.businessPartnerId ?? null,
      commissionBaseRate: partner.commissionBaseRate,
      abn: partner.abn ?? '',
      acn: partner.acn ?? ''
    });
    this.intakesArray.clear();
    (partner.intakes ?? []).forEach(intake => this.intakesArray.push(this.fb.control(intake)));
    this.logoPreviewUrl = partner.logoUrl ?? null;
    this.courses = partner.courses ?? [];
    this.businessPartnerName = partner.businessPartnerName ?? null;
  }

  private applyFormMode(): void {
    if (this.mode === 'view') {
      this.partnerForm.disable({ emitEvent: false });
    } else {
      this.partnerForm.enable({ emitEvent: false });
    }
  }

  enterEditMode(): void {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit education partners.');
      return;
    }
    this.mode = 'edit';
    this.errorMessage = '';
    this.applyFormMode();
  }

  cancelEdit(): void {
    this.mode = 'view';
    this.errorMessage = '';
    this.pendingLogoFile = null;
    if (this.partnerSnapshot) {
      this.applyPartnerToForm(this.partnerSnapshot);
    }
    this.applyFormMode();
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.partnerForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  addIntake(input: HTMLInputElement): void {
    const value = input.value.trim();
    if (!value) {
      return;
    }
    if (!this.intakesArray.value.includes(value)) {
      this.intakesArray.push(this.fb.control(value));
    }
    input.value = '';
  }

  removeIntake(index: number): void {
    this.intakesArray.removeAt(index);
  }

  onLogoSelected(file: File): void {
    this.errorMessage = '';
    this.pendingLogoFile = file;
    this.logoPreviewUrl = URL.createObjectURL(file);
    this.partnerForm.get('logoUrl')?.markAsTouched();
  }

  onSubmitPartner(): void {
    const requiredPermission = this.partnerId ? this.permissions.edit : this.permissions.add;
    if (!requiredPermission) {
      this.notificationService.error(this.partnerId ? 'You do not have permission to edit education partners.' : 'You do not have permission to add education partners.');
      return;
    }

    this.partnerForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.partnerForm.invalid) {
      return;
    }
    if (this.logoMissing) {
      this.errorMessage = 'Logo is required.';
      return;
    }
    if (this.intakesArray.length === 0) {
      this.errorMessage = 'Add at least one intake.';
      return;
    }

    this.isSaving = true;

    if (this.pendingLogoFile) {
      this.isUploadingLogo = true;
      this.apiClient.upload({ data: this.pendingLogoFile, fileName: this.pendingLogoFile.name }).subscribe({
        next: (result) => {
          this.isUploadingLogo = false;
          this.partnerForm.patchValue({ logoUrl: result.url });
          this.pendingLogoFile = null;
          this.savePartner();
        },
        error: (err) => {
          this.isUploadingLogo = false;
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Could not upload the logo. Please try again.');
        }
      });
    } else {
      this.savePartner();
    }
  }

  private savePartner(): void {
    const payload = new CreateEducationPartnerDto(this.partnerForm.value);

    if (this.partnerId) {
      this.apiClient.educationPartnersPUT(this.partnerId, payload).subscribe({
        next: () => {
          this.isSaving = false;
          this.mode = 'view';
          this.notificationService.success('Education partner updated successfully.');
          this.loadPartner(this.partnerId!);
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the education partner. Please try again.');
        }
      });
    } else {
      this.apiClient.educationPartnersPOST(payload).subscribe({
        next: (result) => {
          this.isSaving = false;
          this.notificationService.success('Education partner created successfully. You can now add courses.');
          this.router.navigate(['/staff-portal/education-partners', result.id, 'edit'], { replaceUrl: true });
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the education partner. Please try again.');
        }
      });
    }
  }

  addCourseIntake(input: HTMLInputElement): void {
    const value = input.value.trim();
    if (!value) {
      return;
    }
    const current: string[] = this.courseForm.value.intakes ?? [];
    if (!current.includes(value)) {
      this.courseForm.patchValue({ intakes: [...current, value] });
    }
    input.value = '';
  }

  removeCourseIntake(index: number): void {
    const current: string[] = this.courseForm.value.intakes ?? [];
    this.courseForm.patchValue({ intakes: current.filter((_, i) => i !== index) });
  }

  isCourseFieldInvalid(fieldName: string): boolean {
    const control = this.courseForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  addCourseStudyMode(input: HTMLInputElement): void {
    const value = input.value.trim();
    if (!value) return;
    const current: string[] = this.courseForm.value.studyModes ?? [];
    if (!current.includes(value)) {
      this.courseForm.patchValue({ studyModes: [...current, value] });
    }
    input.value = '';
  }

  removeCourseStudyMode(index: number): void {
    const current: string[] = this.courseForm.value.studyModes ?? [];
    this.courseForm.patchValue({ studyModes: current.filter((_, i) => i !== index) });
  }

  addCourseCampus(input: HTMLInputElement): void {
    const value = input.value.trim();
    if (!value) return;
    const current: string[] = this.courseForm.value.campuses ?? [];
    if (!current.includes(value)) {
      this.courseForm.patchValue({ campuses: [...current, value] });
    }
    input.value = '';
  }

  removeCourseCampus(index: number): void {
    const current: string[] = this.courseForm.value.campuses ?? [];
    this.courseForm.patchValue({ campuses: current.filter((_, i) => i !== index) });
  }

  openAddCourseForm(): void {
    this.editingCourseId = null;
    // Commission base rate defaults from the parent partner's own rate but stays
    // editable — a course can negotiate a different rate than the partner default.
    this.courseForm.reset({
      courseName: '', intakes: [], studyModes: [], campuses: [], tuitionFee: 0, totalTuitionFee: 0, tuitionCurrency: 'AUD',
      courseDurationMonths: null, commissionBaseRate: this.partnerForm.value.commissionBaseRate ?? 0
    });
    this.courseErrorMessage = '';
    this.isCourseFormOpen = true;
  }

  editCourse(course: CourseDto): void {
    this.editingCourseId = course.id ?? null;
    this.courseForm.reset({
      courseName: course.courseName,
      intakes: [...(course.intakes ?? [])],
      studyModes: [...(course.studyModes ?? [])],
      campuses: [...(course.campuses ?? [])],
      tuitionFee: course.tuitionFee,
      totalTuitionFee: course.totalTuitionFee,
      tuitionCurrency: course.tuitionCurrency,
      courseDurationMonths: course.courseDurationMonths ?? null,
      commissionBaseRate: course.commissionBaseRate
    });
    this.courseErrorMessage = '';
    this.isCourseFormOpen = true;
  }

  closeCourseForm(): void {
    this.editingCourseId = null;
    this.courseForm.reset({ courseName: '', intakes: [], studyModes: [], campuses: [], tuitionFee: 0, totalTuitionFee: 0, tuitionCurrency: 'AUD', courseDurationMonths: null, commissionBaseRate: 0 });
    this.courseErrorMessage = '';
    this.isCourseFormOpen = false;
  }

  onSubmitCourse(): void {
    if (!this.partnerId) {
      return;
    }

    this.courseForm.markAllAsTouched();
    this.courseErrorMessage = '';

    const intakes: string[] = this.courseForm.value.intakes ?? [];
    if (this.courseForm.invalid || intakes.length === 0) {
      if (intakes.length === 0) {
        this.courseErrorMessage = 'Select at least one intake.';
      }
      return;
    }

    this.isSavingCourse = true;
    const value = this.courseForm.value;
    const payload = new CreateCourseDto({
      educationPartnerId: this.partnerId,
      courseName: value.courseName,
      intakes: value.intakes,
      studyModes: value.studyModes,
      campuses: value.campuses,
      tuitionFee: value.tuitionFee,
      totalTuitionFee: value.totalTuitionFee,
      tuitionCurrency: value.tuitionCurrency,
      courseDurationMonths: value.courseDurationMonths,
      commissionBaseRate: value.commissionBaseRate
    });

    const request$ = this.editingCourseId
      ? this.apiClient.coursesPUT(this.editingCourseId, payload)
      : this.apiClient.coursesPOST(payload);

    request$.subscribe({
      next: () => {
        this.isSavingCourse = false;
        this.notificationService.success(this.editingCourseId ? 'Course updated successfully.' : 'Course added successfully.');
        this.closeCourseForm();
        this.reloadCourses();
      },
      error: (err) => {
        this.isSavingCourse = false;
        this.courseErrorMessage = extractApiErrorMessage(err, 'Something went wrong saving the course. Please try again.');
      }
    });
  }

  deleteCourse(course: CourseDto): void {
    if (!course.id) {
      return;
    }
    const confirmed = window.confirm(`Delete the course "${course.courseName}"?`);
    if (!confirmed) {
      return;
    }

    this.apiClient.coursesDELETE(course.id).subscribe({
      next: () => {
        this.notificationService.success('Course deleted successfully.');
        this.reloadCourses();
      },
      error: () => {
        this.notificationService.error('Could not delete this course. Please try again.');
      }
    });
  }

  private reloadCourses(): void {
    if (!this.partnerId) {
      return;
    }
    this.apiClient.byPartner(this.partnerId).subscribe({
      next: (courses: CourseDto[]) => {
        this.courses = courses ?? [];
      }
    });
  }
}
