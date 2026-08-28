import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Client, UpdateStudentDto, AddressDto, PersonType } from '@services/api.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { StudentDetailsFormComponent } from '../../../share-component/student-details-form/student-details-form.component';
import { NotificationComponent } from '@generic/notification/notification.component';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-student-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StudentDetailsFormComponent, NotificationComponent],
  templateUrl: './student-edit.component.html',
  styleUrls: ['./student-edit.component.css']
})
export class StudentEditComponent implements OnInit {
  form: FormGroup;
  isSubmitting = false;
  isLoading = false;
  errorMessage = '';
  private studentId!: string;

  // Type is immutable after creation — shown read-only, never sent back on update
  // (see UpdateStudentDto).
  personType: PersonType = PersonType.Student;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.form = StudentDetailsFormComponent.buildFormGroup(this.fb);

    if (!this.authHelper.hasStudentsPermission().edit) {
      this.notificationService.error('You do not have permission to edit contacts.');
      this.router.navigate(['/staff-portal/contacts']);
    }
  }

  get pageTitle(): string {
    return this.personType === PersonType.Customer ? 'Edit Customer' : 'Edit Student';
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.studentId = id;
    this.load();
  }

  private load(): void {
    this.isLoading = true;
    this.apiClient.studentsGET(this.studentId).subscribe({
      next: (student) => {
        this.personType = student.type ?? PersonType.Student;
        this.form.patchValue({
          email: student.email,
          mobile: student.mobile,
          firstName: student.firstName,
          lastName: student.lastName,
          nationality: student.nationality,
          passportNumber: student.passportNumber,
          dateOfBirth: this.toDateInputValue(student.dateOfBirth),
          street: student.address?.street,
          suburb: student.address?.suburb,
          city: student.address?.city,
          state: student.address?.state,
          country: student.address?.country,
          postalCode: student.address?.postalCode
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = extractApiErrorMessage(err, 'Could not load this student.');
      }
    });
  }

  private toDateInputValue(date?: Date): string {
    if (!date) return '';
    const d = new Date(date);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
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

    if (this.form.invalid) return;

    this.isSubmitting = true;
    const v = this.form.value;
    const updateDto = new UpdateStudentDto({
      email: v.email,
      mobile: v.mobile,
      firstName: v.firstName,
      lastName: v.lastName,
      nationality: v.nationality,
      passportNumber: v.passportNumber,
      dateOfBirth: v.dateOfBirth ? new Date(v.dateOfBirth) : undefined,
      address: this.buildAddress()
    });

    this.apiClient.studentsPUT(this.studentId, updateDto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.notificationService.success('Updated successfully.');
        this.router.navigate(['/staff-portal/contacts', this.studentId]);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong updating this record. Please try again.');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/contacts', this.studentId]);
  }
}
