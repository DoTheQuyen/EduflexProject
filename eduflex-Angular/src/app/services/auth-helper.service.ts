import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class AuthHelperService {
  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    const token = localStorage.getItem('authToken');
    return !!token && this.isTokenValid();
  }

  private isTokenValid(): boolean {
    // Add your token validation logic here
    // For now, just return true if token exists
    return true;
  }

  getCurrentUser(): any {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      const userData = localStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  }

  getUserInfo(): any {
    return this.getCurrentUser();
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('userData');
      localStorage.removeItem('rememberMe');
      localStorage.removeItem('userEmail');
    }
    // Optionally navigate using Router here instead of full reload
  }

  getAuthToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('authToken');
  }

  getUserRole(): string {
    const userInfo = this.getCurrentUser();
    return userInfo?.role || 'student';
  }
}
