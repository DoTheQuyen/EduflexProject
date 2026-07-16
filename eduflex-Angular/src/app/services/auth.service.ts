import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { User } from '../models/user';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public readonly currentUser: Observable<User | null> = this.currentUserSubject.asObservable();

  constructor(
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    // initialize only if in browser
    if (isPlatformBrowser(this.platformId)) {
      this.currentUserSubject.next(this.readUserFromStorage());
    }
  }

  // ✅ safe getter for templates
  get isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    const token = localStorage.getItem('access_token');
    if (!token) return false;

    try {
      const decoded: any = jwtDecode(token);
      return (decoded?.exp ?? 0) > Date.now() / 1000;
    } catch {
      return false;
    }
  }

  // ✅ explicit method version
  public isLoggedInNow(): boolean {
    return this.isLoggedIn;
  }

  public getUserInfo(): User | null {
    return isPlatformBrowser(this.platformId) ? this.readUserFromStorage() : null;
  }

  // ✅ set session only in browser
  public setSession(token: string, user: User): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('access_token', token);
      localStorage.setItem('user_info', JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  public clearSession(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('access_token');
      localStorage.removeItem('user_info');
    }
    this.currentUserSubject.next(null);
  }

  public logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  private readUserFromStorage(): User | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const raw = localStorage.getItem('user_info');
    try {
      return raw ? (JSON.parse(raw) as User) : null;
    } catch {
      return null;
    }
  }
}
