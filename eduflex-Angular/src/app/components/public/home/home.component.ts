import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthHelperService } from '../../../services/auth-helper.service';
import { EnquiryModalComponent } from '../enquiry-modal/enquiry-modal.component';
import { FeedbackCarouselComponent } from '../feedback-carousel/feedback-carousel.component';
import { CoursePromotionCarouselComponent } from '../course-promotion-carousel/course-promotion-carousel.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, EnquiryModalComponent, FeedbackCarouselComponent, CoursePromotionCarouselComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit, OnDestroy {
  activeTab: string = 'schools';
  isMobileMenuOpen: boolean = false;
  isHeaderSticky: boolean = false;
  showUserDropdown: boolean = false;
  isEnquiryModalOpen: boolean = false;
  showEnquirySuccess: boolean = false;
  userInfo: any;

  private enquirySuccessTimeout: any;

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

  ngOnDestroy(): void {
    if (this.enquirySuccessTimeout) {
      clearTimeout(this.enquirySuccessTimeout);
    }
  }

  // Check if user is logged in
  get isLoggedIn(): boolean {
    return this.authHelper.isLoggedIn();
  }

  openEnquiryModal(): void {
    this.isEnquiryModalOpen = true;
  }

  closeEnquiryModal(): void {
    this.isEnquiryModalOpen = false;
  }

  onCourseEnquire(): void {
    this.openEnquiryModal();
  }

  onEnquirySubmitted(): void {
    this.isEnquiryModalOpen = false;
    this.showEnquirySuccess = true;
    this.enquirySuccessTimeout = setTimeout(() => {
      this.showEnquirySuccess = false;
    }, 550000);
  }

  dismissEnquirySuccess(): void {
    this.showEnquirySuccess = false;
    if (this.enquirySuccessTimeout) {
      clearTimeout(this.enquirySuccessTimeout);
    }
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