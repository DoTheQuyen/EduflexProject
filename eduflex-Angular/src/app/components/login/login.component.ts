import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { Client, LoginDto, AuthResponseDto } from '@services/public.services';
import { AuthHelperService } from '@services/auth-helper.service';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit, OnDestroy {
  loginForm: FormGroup;
  showPassword = false;
  errorMessage = '';
  successMessage = '';
  isLoading = false;
  private destroy$ = new Subject<void>();
  private hasCheckedInitialAuth = false;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private router: Router,
    private authHelper: AuthHelperService,
    private translate: TranslateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }

  ngOnInit(): void {
    // Detect navigation loops
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd), takeUntil(this.destroy$))
      .subscribe((event: NavigationEnd) => {
        if (event.url === '/login' && this.authHelper.isLoggedIn()) {
          this.forcePortalRedirect();
        }
      });

    this.loadRememberedEmail();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private checkInitialAuth(): void {
    if (this.authHelper.isLoggedIn()) {
      this.forcePortalRedirect();
    } else {
      this.loadRememberedEmail();
    }
  }

  private forcePortalRedirect(): void {
    const currentUser = this.authHelper.getCurrentUser();
    const userRole = currentUser?.role?.toLowerCase() || 'student';
    const portalPath = userRole === 'student' ? '/student-portal' : '/staff-portal';

    this.router.navigateByUrl(portalPath, { replaceUrl: true });
  }

  onSubmit(): void {
    this.loginForm.markAllAsTouched();

    if (this.loginForm.invalid || this.isLoading) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const loginDto: LoginDto = new LoginDto({
      email: this.loginForm.get('email')?.value,
      password: this.loginForm.get('password')?.value
    });

    this.apiClient.login(loginDto).subscribe({
      next: (response: AuthResponseDto) => {
        this.storeAuthData(response);
        this.handleRememberMe(loginDto.email as string);
        this.successMessage = this.translate.instant('LOGIN.LOGIN_SUCCESS');

        setTimeout(() => {
          if (response.mustChangePassword) {
            this.router.navigateByUrl(this.profilePathForCurrentRole(), { replaceUrl: true });
          } else {
            this.forcePortalRedirect();
          }
        }, 800);

        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = this.getErrorMessage(err);
        this.isLoading = false;
        this.loginForm.get('password')?.reset();
      }
    });
  }

  private storeAuthData(authResponse: AuthResponseDto): void {
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.setItem('authToken', authResponse.token || '');
      sessionStorage.setItem('userData', JSON.stringify({
        id: authResponse.userId,
        email: authResponse.email,
        firstName: authResponse.firstName,
        lastName: authResponse.lastName,
        role: authResponse.roleName,
        roleId: authResponse.roleId,
        mustChangePassword: authResponse.mustChangePassword,
        permissions: authResponse.permissions || []
      }));
    }
  }

  private profilePathForCurrentRole(): string {
    const currentUser = this.authHelper.getCurrentUser();
    const userRole = currentUser?.role?.toLowerCase() || 'student';
    return userRole === 'student' ? '/student-portal/profile' : '/staff-portal/profile';
  }

  private handleRememberMe(email: string): void {
    if (!isPlatformBrowser(this.platformId)) return;

    if (this.loginForm.get('rememberMe')?.value) {
      localStorage.setItem('rememberMe', 'true');
      localStorage.setItem('userEmail', email);
    } else {
      localStorage.removeItem('rememberMe');
      localStorage.removeItem('userEmail');
    }
  }

  private loadRememberedEmail(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const remember = localStorage.getItem('rememberMe');
    if (remember === 'true') {
      const savedEmail = localStorage.getItem('userEmail');
      this.loginForm.patchValue({
        rememberMe: true,
        email: savedEmail || ''
      });
    }
  }

  private getErrorMessage(err: any): string {
    if (err.status === 401) return this.translate.instant('LOGIN.ERRORS.UNAUTHORIZED');
    if (err.status === 400) return this.translate.instant('LOGIN.ERRORS.BAD_REQUEST');
    if (err.status === 0) return this.translate.instant('LOGIN.ERRORS.NO_CONNECTION');
    return err.message || this.translate.instant('LOGIN.ERRORS.UNEXPECTED');
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.loginForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  getFieldError(fieldName: string): string {
    const control = this.loginForm.get(fieldName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return this.translate.instant('LOGIN.ERRORS.REQUIRED');
    if (control.errors['email']) return this.translate.instant('LOGIN.ERRORS.EMAIL_INVALID');
    if (control.errors['minlength']) {
      return this.translate.instant('LOGIN.ERRORS.PASSWORD_MIN_LENGTH', { length: control.errors['minlength'].requiredLength });
    }

    return this.translate.instant('LOGIN.ERRORS.INVALID_FIELD');
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    this.errorMessage = this.translate.instant('LOGIN.FORGOT_PASSWORD_MSG');
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  goToHome(): void {
    this.router.navigate(['/']);
  }
}
