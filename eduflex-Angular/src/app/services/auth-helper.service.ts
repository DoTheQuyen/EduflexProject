import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthHelperService {
  isLoggedIn(): boolean {
    const token = localStorage.getItem('authToken');
    // Add token validation if needed (check expiration)
    return !!token && this.isTokenValid();
  }

  private isTokenValid(): boolean {
    // Add your token validation logic here
    // For now, just return true if token exists
    return true;
  }

  getCurrentUser(): any {
    try {
      const userData = localStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch (e) {
      return null;
    }
  }

  getUserInfo(): any {
    return this.getCurrentUser();
  }

  logout(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userData');
    localStorage.removeItem('rememberMe');
    localStorage.removeItem('userEmail');
    // Don't use window.location.href to avoid full page reloads
  }

  getAuthToken(): string | null {
    return localStorage.getItem('authToken');
  }

  getUserRole(): string {
    const userInfo = this.getCurrentUser();
    return userInfo?.role || 'student';
  }
}