import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthHelperService } from '@services/auth-helper.service';
import { RealtimeNotificationService } from '@services/realtime-notification.service';

interface MenuItem {
  title: string;
  icon: string;
  route?: string;
  children?: MenuItem[];
  expanded?: boolean;
  module?: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
})
export class SidebarComponent implements OnChanges {
  @Input() userInfo: any;
  @Input() isCollapsed = false;
  @Output() toggle = new EventEmitter<void>();

  menuItems: MenuItem[] = [];
  moduleCounts$: Observable<Record<string, number>>;

  constructor(
    private authHelper: AuthHelperService,
    private realtimeNotificationService: RealtimeNotificationService,
  ) {
    this.moduleCounts$ = this.realtimeNotificationService.moduleCounts$;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userInfo']) {
      this.setMenuItems();
    }
  }

  onToggleClick(): void {
    this.toggle.emit();
  }

  // A leaf item's count comes straight from its module; a group header's count is the
  // sum of its children's, so the badge still shows on a collapsed/unexpanded group.
  itemCount(item: MenuItem, counts: Record<string, number>): number {
    if (item.module) {
      return counts[item.module] ?? 0;
    }
    if (item.children) {
      return item.children.reduce((sum, child) => sum + this.itemCount(child, counts), 0);
    }
    return 0;
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
        { title: 'Profile', icon: 'person', route: '/student-portal/profile' },
      ];
      return;
    }

    if (
      this.userInfo.role !== 'Staff' &&
      this.userInfo.role !== 'Manager' &&
      this.userInfo.role !== 'Admin'
    ) {
      this.menuItems = [];
      return;
    }

    const items: MenuItem[] = [
      { title: 'Dashboard', icon: 'gauge', route: '/staff-portal/dashboard' },
    ];

    if (this.authHelper.hasEnquiryPermission().view) {
      items.push({
        title: 'Enquiry',
        icon: 'envelope',
        route: '/staff-portal/enquiries',
        module: 'Enquiry',
      });
    }

    // Academic group: Courses, Applications, Enrolments, Students
    const academicChildren: MenuItem[] = [];
    if (this.userInfo.role === 'Staff' || this.userInfo.role === 'Manager') {
      academicChildren.push({ title: 'Courses', icon: 'school', route: '/staff-portal/courses' });
    }
    academicChildren.push({
      title: 'Applications',
      icon: 'clipboard-list',
      route: '/staff-portal/applications',
      module: 'Application',
    });
    if (this.authHelper.hasEnrolmentsPermission().view) {
      academicChildren.push({
        title: 'Enrolments',
        icon: 'user-graduate',
        route: '/staff-portal/enrolments',
        module: 'Enrolment',
      });
    }
    if (this.authHelper.hasStudentsPermission().view) {
      academicChildren.push({ title: 'Students', icon: 'users', route: '/staff-portal/students' });
    }
    if (academicChildren.length) {
      items.push({
        title: 'Academic',
        icon: 'graduation-cap',
        children: academicChildren,
        expanded: false,
      });
    }

    // Migration Cases — the generic, category-agnostic case system (Student/485/Skilled/
    // Partner/Parent/Protection) that runs on VISA Process Templates. Deliberately a
    // standalone top-level item, not nested under Academic (it's not academic-specific)
    // or under Administration (it's an operational workflow, not admin config) — see
    // docs/09-visa-process-config-module-design.md.
    if (this.authHelper.hasMigrationCasesPermission().view) {
      items.push({
        title: 'Migration Cases',
        icon: 'passport',
        route: '/staff-portal/migration-cases',
      });
    }

    // Tasks group: My Tasks (every role — assigner/assignee access is the same for
    // everyone; TaskManagementComponent itself checks TasksView and shows a permission
    // error) and All Tasks (Manager/Admin only, department-scoped server-side too, see
    // TaskItemService.SearchAllTasksAsync).
    const taskChildren: MenuItem[] = [];
    if (this.authHelper.hasTasksPermission().view) {
      taskChildren.push({
        title: 'My Tasks',
        icon: 'list-check',
        route: '/staff-portal/my-tasks',
      });
    }
    if (this.authHelper.hasTasksPermission().viewAll) {
      taskChildren.push({
        title: 'All Tasks',
        icon: 'list-check',
        route: '/staff-portal/tasks',
      });
    }
    if (taskChildren.length) {
      items.push({
        title: 'Tasks',
        icon: 'list-check',
        children: taskChildren,
        expanded: false,
      });
    }

    // Partners group: Education Partners, Business Partners
    const partnersChildren: MenuItem[] = [];
    if (this.authHelper.hasEducationPartnersPermission().view) {
      partnersChildren.push({
        title: 'Education Partners',
        icon: 'handshake',
        route: '/staff-portal/education-partners',
      });
    }
    if (this.authHelper.hasBusinessPartnersPermission().view) {
      partnersChildren.push({
        title: 'Business Partners',
        icon: 'briefcase',
        route: '/staff-portal/business-partners',
      });
    }
    if (partnersChildren.length) {
      items.push({
        title: 'Partners',
        icon: 'people-arrows',
        children: partnersChildren,
        expanded: false,
      });
    }

    // Finance group: Accounts opens straight to its Action Queue tab (only what needs
    // attention now); the All Accounts tab lives inside that same page. Commission
    // Records is the existing per-enrolment partner-commission list (each record is
    // also reachable via the "View Financial Record" button on its source enrolment).
    if (this.authHelper.hasFinancePermission().view) {
      items.push({
        title: 'Finance',
        icon: 'dollar-sign',
        expanded: false,
        children: [
          {
            title: 'Accounts',
            icon: 'users',
            route: '/staff-portal/finance/accounts',
            module: 'Finance',
          },
          {
            title: 'Commission Records',
            icon: 'handshake',
            route: '/staff-portal/financial-records',
          },
        ],
      });
    }

    // Marketing group: Course Promotions, Feedback
    const marketingChildren: MenuItem[] = [];
    if (this.authHelper.hasCoursePromotionsPermission().view) {
      marketingChildren.push({
        title: 'Course Promotions',
        icon: 'bullhorn',
        route: '/staff-portal/course-promotions',
      });
    }
    marketingChildren.push({
      title: 'Feedback',
      icon: 'comment',
      route: '/staff-portal/feedback',
      module: 'Feedback',
    });
    items.push({
      title: 'Marketing',
      icon: 'bullhorn',
      children: marketingChildren,
      expanded: false,
    });

    // template group: Users, Roles
    const templateChildren: MenuItem[] = [];
    // if (this.authHelper.hasSettingsPermission().view) {
    //   templateChildren.push({
    //     title: 'App Settings',
    //     icon: 'sliders',
    //     route: '/staff-portal/settings',
    //   });
    // }
    if (
      (this.userInfo.role === 'Admin' || this.userInfo.role === 'Manager') &&
      this.authHelper.hasDynamicFormsPermission().view
    ) {
      templateChildren.push({
        title: 'Dynamic Forms',
        icon: 'file-lines',
        route: '/staff-portal/dynamic-forms',
      });
    }
    if (this.authHelper.hasVisaProcessTemplatesPermission().view) {
      templateChildren.push({
        title: 'VISA Process Templates',
        icon: 'diagram-project',
        route: '/staff-portal/visa-process-templates',
      });
      templateChildren.push({
        title: 'Practitioner Tags',
        icon: 'tags',
        route: '/staff-portal/practitioner-tags',
      });
    }
    if (this.authHelper.hasEmailTemplatesPermission().view) {
      templateChildren.push({
        title: 'Email Templates',
        icon: 'envelope-open-text',
        route: '/staff-portal/email-templates',
      });
    }
    if (this.authHelper.hasInvoiceTemplatesPermission().view) {
      templateChildren.push({
        title: 'Invoice Templates',
        icon: 'file-invoice-dollar',
        route: '/staff-portal/invoice-templates',
      });
    }
    if (templateChildren.length) {
      items.push({
        title: 'Template',
        icon: 'copy',
        children: templateChildren,
        expanded: false,
      });
    }

    // Setting group: Users, Roles
    const settingChildren: MenuItem[] = [];
    if (this.authHelper.hasUsersPermission().view) {
      settingChildren.push({ title: 'Users', icon: 'users', route: '/staff-portal/users' });
    }
    if (this.authHelper.hasRolesPermission().view) {
      settingChildren.push({ title: 'Roles', icon: 'lock', route: '/staff-portal/roles' });
    }
    // Route guard restricts this to Admin/Manager (app.routes.ts) — checking role
    // here too so the sidebar never shows a link a permission grant alone can't
    // actually open, regardless of what a given Staff account happens to be granted.
    if (
      (this.userInfo.role === 'Admin' || this.userInfo.role === 'Manager') &&
      this.authHelper.hasDepartmentsPermission().view
    ) {
      settingChildren.push({
        title: 'Departments',
        icon: 'sitemap',
        route: '/staff-portal/departments',
      });
    }
    if (this.authHelper.hasSettingsPermission().view) {
      settingChildren.push({
        title: 'App Settings',
        icon: 'sliders',
        route: '/staff-portal/settings',
      });
    }
    // if (this.authHelper.hasDynamicFormsPermission().view) {
    //   settingChildren.push({
    //     title: 'Dynamic Forms',
    //     icon: 'file-lines',
    //     route: '/staff-portal/dynamic-forms',
    //   });
    // }
    // if (this.authHelper.hasVisaProcessTemplatesPermission().view) {
    //   settingChildren.push({
    //     title: 'VISA Process Templates',
    //     icon: 'diagram-project',
    //     route: '/staff-portal/visa-process-templates',
    //   });
    //   settingChildren.push({
    //     title: 'Practitioner Tags',
    //     icon: 'tags',
    //     route: '/staff-portal/practitioner-tags',
    //   });
    // }
    // if (this.authHelper.hasEmailTemplatesPermission().view) {
    //   settingChildren.push({
    //     title: 'Email Templates',
    //     icon: 'envelope-open-text',
    //     route: '/staff-portal/email-templates',
    //   });
    // }
    // if (this.authHelper.hasInvoiceTemplatesPermission().view) {
    //   settingChildren.push({
    //     title: 'Invoice Templates',
    //     icon: 'file-invoice-dollar',
    //     route: '/staff-portal/invoice-templates',
    //   });
    // }
    if (settingChildren.length) {
      items.push({
        title: 'Administration',
        icon: 'gear',
        children: settingChildren,
        expanded: false,
      });
    }

    items.push({ title: 'Profile', icon: 'person', route: '/staff-portal/profile' });

    this.menuItems = items;
  }
}
