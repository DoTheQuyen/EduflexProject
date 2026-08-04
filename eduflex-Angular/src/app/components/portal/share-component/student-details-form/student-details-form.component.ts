import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CountrySelectComponent } from '@generic/country-select/country-select.component';

// Reusable "who is this student" field set — email, mobile, name, nationality,
// passport, DOB and address. Used standalone by the Add Student page today;
// intended to also back the first step of a future staff "Add Application"
// flow and the existing "New Enrolment" form once those are ready to adopt it
// (Enrolment currently has extra fields — middleName, gender, emergency
// contact — this component deliberately doesn't try to cover those).
@Component({
  selector: 'app-student-details-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CountrySelectComponent],
  templateUrl: './student-details-form.component.html',
  styleUrls: ['./student-details-form.component.css']
})
export class StudentDetailsFormComponent {
  @Input() formGroup!: FormGroup;

  static buildFormGroup(fb: FormBuilder): FormGroup {
    return fb.group({
      email: ['', [Validators.required, Validators.email]],
      mobile: ['', [Validators.required, Validators.maxLength(20)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      nationality: ['', [Validators.required, Validators.maxLength(50)]],
      passportNumber: ['', [Validators.required, Validators.maxLength(20)]],
      dateOfBirth: ['', [Validators.required]],
      street: ['', Validators.required],
      suburb: [''],
      city: ['', Validators.required],
      state: [''],
      country: ['', Validators.required],
      postalCode: ['', Validators.required]
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.formGroup.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }
}