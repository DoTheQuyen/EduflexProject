import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthHelperService } from '../../../services/auth-helper.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  activeTab: string = 'schools';
  isMobileMenuOpen: boolean = false;
  isHeaderSticky: boolean = false;
  showUserDropdown: boolean = false;
  userInfo: any;

  constructor(
    private authHelper: AuthHelperService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.authHelper.isLoggedIn()) {
      // User is already logged in → redirect to portal
      this.userInfo = this.authHelper.getCurrentUser();
      const userRole = this.userInfo?.role?.toLowerCase() || 'student';
      const portalPath = userRole === 'student' ? '/student-portal' : '/staff-portal';
      this.router.navigateByUrl(portalPath, { replaceUrl: true });
      return;
    }
  }

  // Check if user is logged in
  get isLoggedIn(): boolean {
    return this.authHelper.isLoggedIn();
  }

  // Navigation methods
  setActiveTab(tab: string): void {
    this.activeTab = tab;
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    this.showUserDropdown = false;
  }

  toggleUserDropdown(): void {
    this.showUserDropdown = !this.showUserDropdown;
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
  }

  closeDropdowns(): void {
    this.showUserDropdown = false;
    this.closeMobileMenu();
  }

  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    this.closeDropdowns();
  }

  // Navigation actions
  goToLogin(): void {
    this.router.navigate(['/login']);
    this.closeDropdowns();
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
    this.closeDropdowns();
  }

  goToDashboard(): void {
    if (this.isLoggedIn) {
      const userRole = this.userInfo?.role?.toLowerCase() || 'student';
      if (userRole === 'student') {
        this.router.navigate(['/student-portal']);
      } else {
        this.router.navigate(['/staff-portal']);
      }
    } else {
      this.goToLogin();
    }
    this.closeDropdowns();
  }

  logout(): void {
    this.authHelper.logout();
    this.closeDropdowns();
  }

  // Host listeners for window events
  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isHeaderSticky = window.scrollY > 100;
    this.showUserDropdown = false;
  }

  @HostListener('window:resize', [])
  onWindowResize() {
    if (window.innerWidth > 768) {
      this.closeMobileMenu();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-dropdown-container') && !target.closest('.user-avatar')) {
      this.showUserDropdown = false;
    }
  }
}
