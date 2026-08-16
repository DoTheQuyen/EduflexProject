import { Component, OnInit, HostListener, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { User } from '../../../models/user';
import { LanguageService } from '../../../services/language.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent implements OnInit {
  currentUser: User | null = null;
  isMobileMenuOpen: boolean = false;
  isUserDropdownOpen: boolean = false;
  isNavbarHidden: boolean = false;
  private lastScrollY = 0;

  constructor(
    public authService: AuthService,
    private router: Router,
    public languageService: LanguageService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    this.authService.currentUser.subscribe(user => {
      this.currentUser = user;
    });
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const currentScrollY = window.scrollY;
    this.isNavbarHidden = currentScrollY > this.lastScrollY && currentScrollY > 80;
    this.lastScrollY = currentScrollY;
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  /** Bound to every link inside the mobile menu, rather than closing on
   *  router navigation — Angular's default onSameUrlNavigation is 'ignore',
   *  so tapping the link for the page you're already on wouldn't have fired
   *  a navigation event at all, and the menu would have stayed open. */
  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
  }

  setLang(lang: string): void {
    this.languageService.setLang(lang);
  }

  toggleUserDropdown(): void {
    this.isUserDropdownOpen = !this.isUserDropdownOpen;
  }

  getInitials(): string {
    if (!this.currentUser) return '?';
    const first = this.currentUser.firstName?.charAt(0) || '';
    const last = this.currentUser.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || 'U';
  }

  getUserName(): string {
    return this.currentUser?.firstName || 'User';
  }

  logout(): void {
    this.authService.logout();
    this.isUserDropdownOpen = false;
    this.router.navigate(['/']);
  }
}