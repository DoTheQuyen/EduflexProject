import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface MenuItem {
  title: string;
  icon: string;
  route: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnChanges {
  @Input() userInfo: any;
  @Input() isCollapsed = false;

  menuItems: MenuItem[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userInfo']) {
      this.setMenuItems();
    }
  }

  toggleSidebar() {
    this.isCollapsed = !this.isCollapsed;
  }
  
  private setMenuItems(): void {
    if (!this.userInfo) {
      this.menuItems = [];
      return;
    }

    if (this.userInfo.role === 'Student') {
      this.menuItems = [
        { title: 'Application', icon: 'assignment', route: '/student-portal/application' },
        { title: 'My Courses', icon: 'book', route: '/my-courses' },
        { title: 'Profile', icon: 'person', route: '/student-portal/profile' }
      ];
    } else if (this.userInfo.role === 'Staff') {
      this.menuItems = [
        { title: 'Dashboard', icon: 'dashboard', route: '/staff-portal/dashboard' },
        { title: 'Courses', icon: 'school', route: '/staff-portal/courses' },
        { title: 'Students', icon: 'people', route: '/staff-portal/students' },
        { title: 'Applications', icon: 'assignment', route: '/staff-portal/applications' },
        { title: 'Feedback', icon: 'comment', route: '/staff-portal/feedback' },
        { title: 'Course Promotions', icon: 'bullhorn', route: '/staff-portal/course-promotions' },
        { title: 'Profile', icon: 'person', route: '/staff-portal/profile' }
      ];
    } else {
      this.menuItems = [];
    }
  }
}