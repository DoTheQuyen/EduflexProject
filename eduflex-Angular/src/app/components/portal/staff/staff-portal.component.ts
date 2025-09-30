import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthHelperService } from '../../../services/auth-helper.service';

@Component({
  selector: 'app-staff-portal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="staff-portal">
      <h1>Staff Portal</h1>
      <p>Welcome to the staff portal</p>
      <button (click)="logout()">Logout</button>
    </div>
  `
})
export class StaffPortalComponent {
  constructor(
    private authHelper: AuthHelperService,
    private router: Router
  ) {}

  logout(): void {
    this.authHelper.logout();
    this.router.navigate(['/']);
  }
}