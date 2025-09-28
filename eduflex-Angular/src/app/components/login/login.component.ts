import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Client, LoginDto, AuthResponseDto } from '../../services/auth.services';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  showPassword = false;
  errorMessage = '';
  successMessage = '';
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }

  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  ngOnInit(): void {
    // Pre-fill email if rememberMe was used
    const remember = localStorage.getItem('rememberMe');
    if (remember === 'true') {
      const savedEmail = localStorage.getItem('userEmail');
      this.loginForm.patchValue({
        rememberMe: true,
        email: savedEmail || ''
      });
    }

    // Redirect if already logged in (check if token exists)
    if (this.isAuthenticated()) {
      this.redirectBasedOnRole();
    }
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.loginForm.markAllAsTouched();
    if (this.loginForm.invalid || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const loginDto: LoginDto = new LoginDto({
      email: this.emailControl?.value,
      password: this.passwordControl?.value
    });

    this.apiClient.login(loginDto).subscribe({
      next: (response: AuthResponseDto) => {
        // Store authentication data
        this.storeAuthData(response);

        this.successMessage = 'Login successful! Redirecting...';

        if (this.loginForm.get('rememberMe')?.value) {
          localStorage.setItem('rememberMe', 'true');
          localStorage.setItem('userEmail', loginDto.email as string);
        } else {
          localStorage.removeItem('rememberMe');
          localStorage.removeItem('userEmail');
        }

        // Redirect based on user role
        setTimeout(() => {
          this.redirectBasedOnRole();
        }, 1500);

        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 
          err.message || 'Invalid email or password. Please try again.';
        this.isLoading = false;
        this.loginForm.get('password')?.reset();
      }
    });
  }

  private storeAuthData(authResponse: AuthResponseDto): void {
    // Store token and user data
    localStorage.setItem('authToken', authResponse.token || '');
    localStorage.setItem('userData', JSON.stringify({
      email: authResponse.email,
      firstName: authResponse.firstName,
      lastName: authResponse.lastName,
      role: authResponse.role
    }));
  }

  private isAuthenticated(): boolean {
    const token = localStorage.getItem('authToken');
    if (!token) return false;

    // Optional: Check token expiration if you have expiry info
    try {
      const userData = localStorage.getItem('userData');
      if (userData) {
        const user = JSON.parse(userData);
        // You could add additional checks here (token expiry, etc.)
        return !!user && !!token;
      }
    } catch (e) {
      console.error('Error parsing user data:', e);
    }
    
    return false;
  }

  private getCurrentUser(): any {
    try {
      const userData = localStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch (e) {
      console.error('Error getting current user:', e);
      return null;
    }
  }

  private redirectBasedOnRole(): void {
    const currentUser = this.getCurrentUser();
    
    if (!currentUser) {
      this.router.navigate(['/']);
      return;
    }

    const userRole = currentUser.role?.toLowerCase() || 'student';

    switch (userRole) {
      case 'admin':
      case 'staff':
      case 'consultant':
      case 'employee':
        this.router.navigate(['/staff-portal']);
        break;
      case 'student':
        this.router.navigate(['/student-portal']);
        break;
      default:
        this.router.navigate(['/']);
        break;
    }
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    this.errorMessage =
      'Password reset functionality coming soon! Please contact support.';
  }

  signInWithGoogle(): void {
    this.errorMessage = 'Google sign-in functionality coming soon!';
  }

  signInWithMicrosoft(): void {
    this.errorMessage = 'Microsoft sign-in functionality coming soon!';
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.loginForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  getFieldError(fieldName: string): string {
    const control = this.loginForm.get(fieldName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return 'This field is required';
    if (control.errors['email']) return 'Please enter a valid email address';
    if (control.errors['minlength'])
      return `Password must be at least ${control.errors['minlength'].requiredLength} characters`;

    return 'Invalid field';
  }
}