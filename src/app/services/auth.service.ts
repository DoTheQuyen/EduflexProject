// // src/app/services/auth.service.ts
// import { Injectable } from '@angular/core';
// import { Router } from '@angular/router';
// import { jwtDecode } from 'jwt-decode';

// @Injectable({
//   providedIn: 'root'
// })
// export class AuthService {
//   constructor(private router: Router) {}

//   isLoggedIn(): boolean {
//     const token = localStorage.getItem('access_token');
//     if (!token) return false;

//     try {
//       const decoded: any = jwtDecode(token);
//       const currentTime = Date.now() / 1000;
//       return decoded.exp > currentTime;
//     } catch (error) {
//       return false;
//     }
//   }

//   logout(): void {
//     localStorage.removeItem('access_token');
//     localStorage.removeItem('user_info');
//     this.router.navigate(['/login']);
//   }

//   getUserInfo(): any {
//     const userInfo = localStorage.getItem('user_info');
//     return userInfo ? JSON.parse(userInfo) : null;
//   }
// }

// src/app/services/auth.service.ts
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { User } from '../models/user'; // adjust path if your models folder differs

@Injectable({ providedIn: 'root' })
export class AuthService {
  // current user stream the navbar can subscribe to
  private currentUserSubject = new BehaviorSubject<User | null>(this.readUserFromStorage());
  public readonly currentUser: Observable<User | null> = this.currentUserSubject.asObservable();

  constructor(private router: Router) {}

  // allow template usage: *ngIf="authService.isLoggedIn"
  get isLoggedIn(): boolean {
    const token = localStorage.getItem('access_token');
    if (!token) return false;
    try {
      const decoded: any = jwtDecode(token);
      return (decoded?.exp ?? 0) > (Date.now() / 1000);
    } catch {
      return false;
    }
  }

  // optional method version if you need to call it from TS
  public isLoggedInNow(): boolean { return this.isLoggedIn; }

  public getUserInfo(): User | null { return this.readUserFromStorage(); }

  // call this after a successful login/refresh to persist and broadcast the user
  public setSession(token: string, user: User): void {
    localStorage.setItem('access_token', token);
    localStorage.setItem('user_info', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  public clearSession(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('user_info');
    this.currentUserSubject.next(null);
  }

  public logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  private readUserFromStorage(): User | null {
    const raw = localStorage.getItem('user_info');
    try { return raw ? (JSON.parse(raw) as User) : null; } catch { return null; }
  }
}
