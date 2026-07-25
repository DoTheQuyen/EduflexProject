import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
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
  @Output() toggle = new EventEmitter<void>();

  menuItems: MenuItem[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userInfo']) {
      this.setMenuItems();
    }
  }

  onToggleClick(): void {
    this.toggle.emit();
  }

  private setMenuItems(): void {
    if (!this.userInfo) {
      this.menuItems = [];
      return;
    }

    if (this.userInfo.role === 'Student') {
      this.menuItems = [
        { title: 'Application', icon: 'clipboard-list', route: '/student-portal/application' },
        { title: 'My Courses', icon: 'book', route: '/my-courses' },
        { title: 'Profile', icon: 'person', route: '/student-portal/profile' }
      ];
    } else if (this.userInfo.role === 'Staff' || this.userInfo.role === 'Manager') {
      this.menuItems = [
        { title: 'Dashboard', icon: 'gauge', route: '/staff-portal/dashboard' },
        { title: 'Courses', icon: 'school', route: '/staff-portal/courses' },
        { title: 'Students', icon: 'users', route: '/staff-portal/students' },
        { title: 'Applications', icon: 'clipboard-list', route: '/staff-portal/applications' },
        { title: 'Feedback', icon: 'comment', route: '/staff-portal/feedback' },
        { title: 'Course Promotions', icon: 'bullhorn', route: '/staff-portal/course-promotions' },
        { title: 'Profile', icon: 'person', route: '/staff-portal/profile' }
      ];
    } else if (this.userInfo.role === 'Admin') {
      this.menuItems = [
        { title: 'Dashboard', icon: 'gauge', route: '/staff-portal/dashboard' },
        { title: 'Applications', icon: 'clipboard-list', route: '/staff-portal/applications' },
        { title: 'Feedback', icon: 'comment', route: '/staff-portal/feedback' },
        { title: 'Course Promotions', icon: 'bullhorn', route: '/staff-portal/course-promotions' },
        { title: 'Roles', icon: 'lock', route: '/staff-portal/roles' },
        { title: 'Users', icon: 'users', route: '/staff-portal/users' },
        { title: 'Profile', icon: 'person', route: '/staff-portal/profile' }
      ];
    } else {
      this.menuItems = [];
    }
  }
}