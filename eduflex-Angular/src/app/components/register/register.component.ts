import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
// import { AuthServices } from '../../services/auth.services';

@Component({
  selector: 'app-register',
  standalone: true,
   imports: [
    CommonModule,
    ReactiveFormsModule, // ✅ Add this import
    RouterModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  showPassword: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    // private authServices: AuthServices,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9\s\-\(\)]+$/)]],
      dateOfBirth: ['', [Validators.required]],
      nationality: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      agreeTerms: [false, [Validators.requiredTrue]],
      newsletter: [true]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    // If user is already logged in, redirect to dashboard
    if (this.authService.isLoggedIn) {
      this.router.navigate(['/dashboard']);
    }
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
    } else {
      confirmPassword?.setErrors(null);
    }
    
    return null;
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.registerForm.markAllAsTouched();

    if (this.registerForm.invalid || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const registerData = this.registerForm.value;
    
    // this.authServices.register(registerData).subscribe({
    //   next: (response) => {
    //     if (response){

    //     }
    //     this.successMessage = 'Account created successfully! Redirecting to dashboard...';
    //     setTimeout(() => {
    //       this.router.navigate(['/dashboard']);
    //     }, 2000);
    //   },
    //   error: (error) => {
    //     this.isLoading = false;
    //     this.errorMessage = error.error?.message || 'Registration failed. Please try again.';
    //   }
    // });
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.registerForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  getFieldError(fieldName: string): string {
    const control = this.registerForm.get(fieldName);
    
    if (!control || !control.errors || !control.touched) return '';
    
    if (control.errors['required']) {
      return 'This field is required';
    }
    
    if (control.errors['email']) {
      return 'Please enter a valid email address';
    }
    
    if (control.errors['minlength']) {
      return `Minimum ${control.errors['minlength'].requiredLength} characters required`;
    }
    
    if (control.errors['pattern']) {
      return 'Please enter a valid phone number';
    }
    
    if (control.errors['passwordMismatch']) {
      return 'Passwords do not match';
    }
    
    return 'Invalid field';
  }
}