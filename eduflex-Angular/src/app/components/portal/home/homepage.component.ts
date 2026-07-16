import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { AuthHelperService } from '../../../services/auth-helper.service';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent], // Remove ApplicationComponent from imports
  templateUrl: './homepage.component.html',
  // styleUrls: ['./homepage.component.css']
})
export class HomepageComponent implements OnInit {
  userInfo: any;
  isSidebarCollapsed = false;

  constructor(
    private authHelper: AuthHelperService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (!this.authHelper.isLoggedIn()) {
      this.authHelper.logout();
      this.router.navigate(['/login'], { replaceUrl: true });
      return;
    }

    this.userInfo = this.authHelper.getCurrentUser();
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  logout(): void {
    this.authHelper.logout();
    this.router.navigate(['/login'], { replaceUrl: true });
  }
}