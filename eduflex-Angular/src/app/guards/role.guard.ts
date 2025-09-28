import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthHelperService } from '../services/auth-helper.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private authHelper: AuthHelperService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const expectedRoles = route.data['roles'] as Array<string>;
    
    if (!this.authHelper.isLoggedIn()) {
      this.router.navigate(['/login']);
      return false;
    }

    const userRole = this.authHelper.getUserRole();
    
    if (expectedRoles.includes(userRole)) {
      return true;
    } else {
      // Redirect to appropriate portal based on role
      if (userRole === 'student') {
        this.router.navigate(['/student-portal']);
      } else {
        this.router.navigate(['/staff-portal']);
      }
      return false;
    }
  }
}