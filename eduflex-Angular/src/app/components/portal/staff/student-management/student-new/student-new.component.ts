import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Client, CreateStudentDto, CheckDuplicateStudentDto, DuplicateCheckResultDto, AddressDto } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { StudentDetailsFormComponent } from '../../../share-component/student-details-form/student-details-form.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-student-new',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StudentDetailsFormComponent, NotificationComponent],
  templateUrl: './student-new.component.html',
  styleUrls: ['./student-new.component.css']
})
export class StudentNewComponent {
  form: FormGroup;
  isSubmitting = false;
  errorMessage = '';

  duplicateWarning: DuplicateCheckResultDto | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.form = StudentDetailsFormComponent.buildFormGroup(this.fb);

    if (!this.authHelper.hasStudentsPermission().add) {
      this.notificationService.error('You do not have permission to add students.');
      this.router.navigate(['/staff-portal/students']);
    }
  }

  private buildAddress(): AddressDto {
    const v = this.form.value;
    return new AddressDto({
      street: v.street,
      suburb: v.suburb || undefined,
      city: v.city,
      state: v.state || undefined,
      country: v.country,
      postalCode: v.postalCode
    });
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    this.errorMessage = '';
    this.duplicateWarning = null;

    if (this.form.invalid) {
      return;
    }

    this.checkDuplicateThenCreate();
  }

  private checkDuplicateThenCreate(): void {
    this.isSubmitting = true;
    const v = this.form.value;
    const checkDto = new CheckDuplicateStudentDto({
      email: v.email,
      mobile: v.mobile,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      passportNumber: v.passportNumber
    });

    this.apiClient.checkDuplicate(checkDto).subscribe({
      next: (result) => {
        this.isSubmitting = false;
        if (result.isDuplicate) {
          this.duplicateWarning = result;
        } else {
          this.submitCreate();
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Could not check for duplicates. Please try again.');
      }
    });
  }

  private submitCreate(): void {
    this.isSubmitting = true;
    const v = this.form.value;
    const createDto = new CreateStudentDto({
      email: v.email,
      mobile: v.mobile,
      firstName: v.firstName,
      lastName: v.lastName,
      nationality: v.nationality,
      passportNumber: v.passportNumber,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      address: this.buildAddress()
    });

    this.apiClient.studentsPOST(createDto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.notificationService.success('Student created successfully. They have been emailed their login details.');
        this.router.navigate(['/staff-portal/students']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong creating this student. Please try again.');
      }
    });
  }

  reactivateFromDuplicate(): void {
    if (!this.duplicateWarning?.existingStudentId) return;

    this.isSubmitting = true;
    this.apiClient.reactivate(this.duplicateWarning.existingStudentId).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.notificationService.success('Existing student reactivated.');
        this.router.navigate(['/staff-portal/students']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Could not reactivate this student. Please try again.');
      }
    });
  }

  dismissDuplicateWarning(): void {
    this.duplicateWarning = null;
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/students']);
  }
}
