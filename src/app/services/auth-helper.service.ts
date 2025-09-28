import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthHelperService {
  
  constructor() { }

  getAuthToken(): string | null {
    return localStorage.getItem('authToken');
  }

  isLoggedIn(): boolean {
    const token = this.getAuthToken();
    return !!token; // Simple check - you can add token expiration validation
  }

  getCurrentUser(): any {
    try {
      const userData = localStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch (e) {
      console.error('Error parsing user data:', e);
      return null;
    }
  }

  getUserRole(): string {
    const user = this.getCurrentUser();
    return user?.role?.toLowerCase() || 'student';
  }

  storeAuthData(token: string, userData: any): void {
    localStorage.setItem('authToken', token);
    localStorage.setItem('userData', JSON.stringify(userData));
  }

  clearAuthData(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userData');
    localStorage.removeItem('rememberMe');
    localStorage.removeItem('userEmail');
  }

  hasRole(requiredRole: string | string[]): boolean {
    const userRole = this.getUserRole();
    
    if (Array.isArray(requiredRole)) {
      return requiredRole.includes(userRole);
    }
    
    return userRole === requiredRole;
  }
}