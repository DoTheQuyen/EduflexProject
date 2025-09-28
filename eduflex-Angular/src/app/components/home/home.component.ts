import { Component, HostListener, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { UserInitialsPipe } from '../../pipes/user-initials.pipe';
import { CommonModule } from '@angular/common';
import { RouterModule, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-home',
  standalone: true,
   imports: [CommonModule, RouterModule, UserInitialsPipe],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  activeTab: string = 'schools';
  isMobileMenuOpen: boolean = false;
  isHeaderSticky: boolean = false;
  showUserDropdown: boolean = false;
  currentRoute: string = '';

  constructor(
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
     // Track current route for active link highlighting
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.currentRoute = event.url;
      });
  }

   // Check if a route is active
  isRouteActive(route: string): boolean {
    if (route === '/') {
      return this.currentRoute === '/';
    }
    return this.currentRoute.startsWith(route);
  }

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

  logout(): void {
    this.authService.logout();
    this.closeDropdowns();
    this.router.navigate(['/']);
  }

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