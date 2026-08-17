import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { QuillModule } from 'ngx-quill';
import { RICH_TEXT_QUILL_MODULES } from '@app/shared/utils/quill-toolbar.util';
import { Client, EnquiryDto, EnquiryStatusDto, EnquiryEnums } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';
import { formatDateTime } from '../../../../../shared/utils/date-time.util';

@Component({
  selector: 'app-enquiry-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, QuillModule, RouterLink],
  templateUrl: './enquiry-detail.component.html',
  styleUrls: ['./enquiry-detail.component.css']
})
export class EnquiryDetailComponent implements OnInit {
  readonly quillModules = RICH_TEXT_QUILL_MODULES;

  enquiry: EnquiryDto | null = null;
  statusOptions: EnquiryStatusDto[] = [];
  isLoading = true;
  isSubmitting = false;
  errorMessage = '';
  permissions!: ModulePermissions;
  private returnTab: string | null = null;

  responseForm: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasEnquiryPermission();

    this.responseForm = this.fb.group({
      status: ['', [Validators.required]],
      response: ['', [Validators.maxLength(5000)]]
    }, { validators: EnquiryDetailComponent.respondingValidator });
  }

  private static respondingValidator(group: AbstractControl): ValidationErrors | null {
    const status = group.get('status')?.value;
    const response = (group.get('response')?.value ?? '').trim();

    if (status === EnquiryEnums.New) {
      return { statusUnchanged: true };
    }
    if (!response) {
      return { responseRequired: true };
    }
    return null;
  }

  get responseCharsLeft(): number {
    const length = this.responseForm.get('response')?.value?.length ?? 0;
    return 5000 - length;
  }

  get responseAlreadySet(): boolean {
    return !!this.enquiry?.response;
  }

  ngOnInit(): void {
    this.returnTab = this.route.snapshot.queryParamMap.get('tab');
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.goBack();
      return;
    }
    this.loadStatuses();
    this.loadEnquiry(id);
  }

  private navigateBackToList(): void {
    this.router.navigate(['/staff-portal/enquiries'], {
      queryParams: this.returnTab ? { tab: this.returnTab } : {}
    });
  }

  loadStatuses(): void {
    this.apiClient.enquiryStatuses().subscribe({
      next: (statuses) => { this.statusOptions = statuses; },
      error: () => {}
    });
  }

  loadEnquiry(id: string): void {
    this.isLoading = true;
    this.apiClient.enquiriesGET(id).subscribe({
      next: (enquiry) => {
        this.enquiry = enquiry;
        this.responseForm.reset({
          status: enquiry.status,
          response: enquiry.response ?? ''
        });
        if (enquiry.response) {
          this.responseForm.get('response')?.disable();
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this enquiry.');
        this.navigateBackToList();
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.responseForm.get(fieldName);
    if (control?.invalid && control?.touched) {
      return true;
    }
    if (fieldName === 'status' && this.responseForm.errors?.['statusUnchanged'] && control?.touched) {
      return true;
    }
    if (fieldName === 'response' && this.responseForm.errors?.['responseRequired'] && control?.touched) {
      return true;
    }
    return false;
  }

  getFieldError(fieldName: string): string {
    const control = this.responseForm.get(fieldName);
    if (!control) return '';

    if (fieldName === 'status') {
      if (control.errors?.['required']) return 'Status is required';
      if (this.responseForm.errors?.['statusUnchanged'] && control.touched) {
        return 'Status must be changed when responding to customer';
      }
    }

    if (fieldName === 'response') {
      if (control.errors?.['maxlength']) {
        return `Response must not exceed ${control.errors['maxlength'].requiredLength} characters`;
      }
      if (this.responseForm.errors?.['responseRequired'] && control.touched) {
        return 'A response message is required when marking an enquiry as Responded';
      }
    }

    return '';
  }

  formatDate(value: Date | string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  onSubmit(): void {
    if (!this.permissions.edit || !this.enquiry?.id) {
      this.notificationService.error('You do not have permission to respond to enquiries.');
      return;
    }

    if (this.responseForm.value.status === EnquiryEnums.Converted) {
      // Converting isn't a plain status update — it creates the student user, application and
      // enrolment record, and the enquiry itself flips to Converted as part of that flow (see
      // EnrolmentService.CreateFromEnquiryAsync). Hand off to the enrolment creation form instead.
      this.router.navigate(['/staff-portal/enrolments/new'], { queryParams: { enquiryId: this.enquiry.id } });
      return;
    }

    this.responseForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.responseForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const payload = new EnquiryDto({
      ...this.enquiry,
      status: this.responseForm.value.status,
      response: this.responseForm.getRawValue().response
    });

    this.apiClient.enquiriesPUT(this.enquiry.id, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.notificationService.success('Enquiry updated successfully.');
        this.navigateBackToList();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the enquiry. Please try again.');
      }
    });
  }

  goBack(): void {
    this.navigateBackToList();
  }
}