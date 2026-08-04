import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthHelperService } from '@services/auth-helper.service';

interface MenuItem {
  title: string;
  icon: string;
  route?: string;
  children?: MenuItem[];
  expanded?: boolean;
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

  constructor(private authHelper: AuthHelperService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userInfo']) {
      this.setMenuItems();
    }
  }

  onToggleClick(): void {
    this.toggle.emit();
  }

onGroupHeaderClick(item: MenuItem): void {
  if (this.isCollapsed) {
    this.toggle.emit();
    item.expanded = true;
    return;
  }
  item.expanded = !item.expanded;
}

  private setMenuItems(): void {
    if (!this.userInfo) {
      this.menuItems = [];
      return;
    }

    if (this.userInfo.role === 'Student') {
      this.menuItems = [
        { title: 'Dashboard', icon: 'gauge', route: '/student-portal/dashboard' },
        { title: 'Application', icon: 'clipboard-list', route: '/student-portal/application' },
        { title: 'My Courses', icon: 'book', route: '/my-courses' },
        { title: 'Profile', icon: 'person', route: '/student-portal/profile' }
      ];
      return;
    }

    if (this.userInfo.role !== 'Staff' && this.userInfo.role !== 'Manager' && this.userInfo.role !== 'Admin') {
      this.menuItems = [];
      return;
    }

    const items: MenuItem[] = [
      { title: 'Dashboard', icon: 'gauge', route: '/staff-portal/dashboard' }
    ];

    if (this.authHelper.hasEnquiryPermission().view) {
      items.push({ title: 'Enquiry', icon: 'envelope', route: '/staff-portal/enquiries' });
    }

    // Academic group: Courses, Applications, Enrolments, Students
    const academicChildren: MenuItem[] = [];
    if (this.userInfo.role === 'Staff' || this.userInfo.role === 'Manager') {
      academicChildren.push({ title: 'Courses', icon: 'school', route: '/staff-portal/courses' });
    }
    academicChildren.push({ title: 'Applications', icon: 'clipboard-list', route: '/staff-portal/applications' });
    if (this.authHelper.hasEnrolmentsPermission().view) {
      academicChildren.push({ title: 'Enrolments', icon: 'user-graduate', route: '/staff-portal/enrolments' });
    }
    if (this.authHelper.hasStudentsPermission().view) {
      academicChildren.push({ title: 'Students', icon: 'users', route: '/staff-portal/students' });
    }
    if (academicChildren.length) {
      items.push({ title: 'Academic', icon: 'graduation-cap', children: academicChildren, expanded: false });
    }

    // Partners group: Education Partners, Business Partners
    const partnersChildren: MenuItem[] = [];
    if (this.authHelper.hasEducationPartnersPermission().view) {
      partnersChildren.push({ title: 'Education Partners', icon: 'handshake', route: '/staff-portal/education-partners' });
    }
    if (this.authHelper.hasBusinessPartnersPermission().view) {
      partnersChildren.push({ title: 'Business Partners', icon: 'briefcase', route: '/staff-portal/business-partners' });
    }
    if (partnersChildren.length) {
      items.push({ title: 'Partners', icon: 'people-arrows', children: partnersChildren, expanded: false });
    }

    // Finance group: standalone list of all financial records, plus each one is also
    // reachable via the "View Financial Record" button on its source enrolment.
    if (this.authHelper.hasFinancePermission().view) {
      items.push({ title: 'Finance', icon: 'dollar-sign', route: '/staff-portal/financial-records' });
    }

    // Marketing group: Course Promotions, Feedback
    const marketingChildren: MenuItem[] = [];
    if (this.authHelper.hasCoursePromotionsPermission().view) {
      marketingChildren.push({ title: 'Course Promotions', icon: 'bullhorn', route: '/staff-portal/course-promotions' });
    }
    marketingChildren.push({ title: 'Feedback', icon: 'comment', route: '/staff-portal/feedback' });
    items.push({ title: 'Marketing', icon: 'bullhorn', children: marketingChildren, expanded: false });

    // Setting group: Users, Roles
    const settingChildren: MenuItem[] = [];
    if (this.authHelper.hasUsersPermission().view) {
      settingChildren.push({ title: 'Users', icon: 'users', route: '/staff-portal/users' });
    }
        if (this.authHelper.hasRolesPermission().view) {
      settingChildren.push({ title: 'Roles', icon: 'lock', route: '/staff-portal/roles' });
    }
    if (this.authHelper.hasDepartmentsPermission().view) {
      settingChildren.push({ title: 'Departments', icon: 'sitemap', route: '/staff-portal/departments' });
    }
    if (this.authHelper.hasSettingsPermission().view) {
      settingChildren.push({ title: 'App Settings', icon: 'sliders', route: '/staff-portal/settings' });
    }
    if (this.authHelper.hasDynamicFormsPermission().view) {
      settingChildren.push({ title: 'Dynamic Forms', icon: 'file-lines', route: '/staff-portal/dynamic-forms' });
    }
    if (this.authHelper.hasEmailTemplatesPermission().view) {
      settingChildren.push({ title: 'Email Templates', icon: 'envelope-open-text', route: '/staff-portal/email-templates' });
    }
    if (this.authHelper.hasInvoiceTemplatesPermission().view) {
      settingChildren.push({ title: 'Invoice Templates', icon: 'file-invoice-dollar', route: '/staff-portal/invoice-templates' });
    }
    if (settingChildren.length) {
      items.push({ title: 'Administration', icon: 'gear', children: settingChildren, expanded: false });
    }

    items.push({ title: 'Profile', icon: 'person', route: '/staff-portal/profile' });

    this.menuItems = items;
  }
}