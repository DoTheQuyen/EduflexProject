import { Component, ElementRef, EventEmitter, Input, OnDestroy, Output, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, CreateEnquiryDto, CoursePromotionDto } from '@services/content.services';
import { environment } from '../../../environments/environment';
import { extractApiErrorMessage } from '../../../shared/utils/api-error.util';
import { TranslatePipe } from '@ngx-translate/core';


declare const grecaptcha: any;

@Component({
  selector: 'app-enquiry-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './enquiry-modal.component.html',
  styleUrls: ['./enquiry-modal.component.css']
})
export class EnquiryModalComponent implements AfterViewInit, OnDestroy {
  @Input() coursePromotion?: CoursePromotionDto;
  @Output() closed = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<void>();
  @ViewChild('recaptchaContainer') recaptchaContainer!: ElementRef<HTMLDivElement>;

  enquiryForm: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  recaptchaToken = '';

  private recaptchaWidgetId: number | null = null;
  private recaptchaPollHandle: any;

  constructor(private fb: FormBuilder, private apiClient: Client) {
    this.enquiryForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      middleName: ['', [Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
      mobile: ['', [Validators.required, Validators.pattern(/^\+?[0-9\s\-()]{7,20}$/)]],
      enquiry: ['', [Validators.required, Validators.maxLength(2000)]]
    });
  }

  ngAfterViewInit(): void {
    this.waitForRecaptchaAndRender();
  }

  ngOnDestroy(): void {
    if (this.recaptchaPollHandle) {
      clearInterval(this.recaptchaPollHandle);
    }
  }

  private waitForRecaptchaAndRender(): void {
    this.recaptchaPollHandle = setInterval(() => {
      const g = (window as any).grecaptcha?.enterprise;
      if (g && typeof g.render === 'function' && this.recaptchaContainer) {
        clearInterval(this.recaptchaPollHandle);
        this.recaptchaWidgetId = g.render(this.recaptchaContainer.nativeElement, {
          sitekey: environment.recaptchaSiteKey,
          callback: (token: string) => {
            this.recaptchaToken = token;
          },
          'expired-callback': () => {
            this.recaptchaToken = '';
          }
        });
      }
    }, 200);
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.enquiryForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  getFieldError(fieldName: string): string {
    const control = this.enquiryForm.get(fieldName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return 'This field is required';
    if (control.errors['email']) return 'Please enter a valid email address';
    if (control.errors['pattern']) return 'Please enter a valid mobile number';
    if (control.errors['maxlength']) return `Maximum ${control.errors['maxlength'].requiredLength} characters`;

    return 'Invalid field';
  }

  onBackdropClick(): void {
    this.close();
  }

  close(): void {
    this.closed.emit();
  }

  onSubmit(): void {
    this.enquiryForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.enquiryForm.invalid) {
      return;
    }

    if (!this.recaptchaToken) {
      this.errorMessage = 'Please verify that you are not a robot';
      return;
    }

    this.isSubmitting = true;

   const { firstName, middleName, lastName, email, mobile, enquiry } = this.enquiryForm.value;
    const subject = this.coursePromotion
      ? `Course: ${this.coursePromotion.courseName} (${this.coursePromotion.universityName})`
      : undefined;

    const payload = new CreateEnquiryDto({
      firstName, middleName, lastName, email, mobile, enquiry, subject,
      coursePromotionId: this.coursePromotion?.id,
      recaptchaToken: this.recaptchaToken
    });

    this.apiClient.enquiries(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.submitted.emit();
        this.close();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong submitting your enquiry. Please try again.');
       if (this.recaptchaWidgetId !== null && (window as any).grecaptcha?.enterprise) {
  (window as any).grecaptcha.enterprise.reset(this.recaptchaWidgetId);
          this.recaptchaToken = '';
        }
      }
    });
  }
}
