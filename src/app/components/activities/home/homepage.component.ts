import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

interface MenuItem {
  title: string;
  icon: string;
  route: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet], 
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.css']
})
export class HomepageComponent implements OnInit {
  userInfo: any;
  isSidebarCollapsed = false;

  menuItems: MenuItem[] = [
    {
      title: 'Dashboard',
      icon: 'dashboard',
      route: '/dashboard'
    },
    {
      title: 'Courses',
      icon: 'school',
      route: '/courses',
      children: [
        { title: 'All Courses', icon: 'list', route: '/courses' },
        { title: 'My Courses', icon: 'book', route: '/my-courses' }
      ]
    },
    {
      title: 'Students',
      icon: 'people',
      route: '/students'
    },
    {
      title: 'Teachers',
      icon: 'person',
      route: '/teachers'
    },
    {
      title: 'Settings',
      icon: 'settings',
      route: '/settings'
    }
  ];

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.authService.logout();
      return;
    }

    this.userInfo = this.authService.getUserInfo();
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  logout(): void {
    this.authService.logout();
  }
}