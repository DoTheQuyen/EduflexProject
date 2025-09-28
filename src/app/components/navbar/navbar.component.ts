import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router'; // Import RouterModule
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/user';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule], // Add RouterModule here
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  currentUser: User | null = null;
  isMobileMenuOpen: boolean = false;
  isUserDropdownOpen: boolean = false;

  constructor(public authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.authService.currentUser.subscribe(user => {
      this.currentUser = user;
    });
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
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