import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/public/home/home.component').then(m => m.HomeComponent)
  }, // Public home page
  {
    path: 'about',
    loadComponent: () => import('./components/public/about/about.component').then(m => m.AboutComponent)
  }, // Public about page
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent)
  }, // Public login page
  {
    path: 'student-handbook',
    loadComponent: () => import('./components/public/student-handbook/student-handbook.component').then(m => m.StudentHandbookComponent)
  }, // Public student handbook page
  {
    path: 'register',
    loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent)
  },

  // Student portal (parent)
  {
    path: 'student-portal',
    loadComponent: () => import('./components/portal/home/homepage.component').then(m => m.HomepageComponent),
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./components/portal/student/dashboard/dashboard.component').then(m => m.StudentDashboardComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Dashboard' }
      },
      {
        path: 'application',
        loadComponent: () => import('./components/portal/student/applications/application.component').then(m => m.ApplicationComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application' }
      },
      {
        path: 'application/new',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Application' }
      },
      {
        path: 'application/:id',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application Detail' }
      },
            {
        path: 'application/:id/forms/:formId',
        loadComponent: () => import('./components/portal/student/applications/application-form-fill/application-form-fill.component').then(m => m.ApplicationFormFillComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Fill Form' }
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile' }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' } // default child
    ]
  },

  // Staff portal
  {
    path: 'staff-portal',
    loadComponent: () => import('./components/portal/home/homepage.component').then(m => m.HomepageComponent),
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./components/portal/staff/dashboard/dashboard.component').then(m => m.DashboardComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Dashboard' }
      },
      {
        path: 'applications',
        loadComponent: () => import('./components/portal/student/applications/application.component').then(m => m.ApplicationComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Applications' }
      },
      {
        path: 'applications/:id',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application Detail' }
      },
      {
        path: 'feedback',
        loadComponent: () => import('./components/portal/staff/feedback-management/feedback-management.component').then(m => m.FeedbackManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Feedback' }
      },
      {
        path: 'course-promotions',
        loadComponent: () => import('./components/portal/staff/course-promotion-management/course-promotion-management.component').then(m => m.CoursePromotionManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Course Promotions' }
      },
      {
        path: 'enquiries',
        loadComponent: () => import('./components/portal/staff/enquiries/enquiry-management/enquiry-management.component').then(m => m.EnquiryManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enquiries' }
      },
      {
        path: 'enquiries/:id',
        loadComponent: () => import('./components/portal/staff/enquiries/enquiry-details/enquiry-detail.component').then(m => m.EnquiryDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enquiry Detail' }
      },
      {
        path: 'enrolments',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-management/enrolment-management.component').then(m => m.EnrolmentManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enrolments' }
      },
      {
        path: 'enrolments/new',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-new/enrolment-new.component').then(m => m.EnrolmentNewComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Enrolment' }
      },
      {
        path: 'enrolments/:id',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-detail/enrolment-detail.component').then(m => m.EnrolmentDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enrolment Detail' }
      },
      {
        path: 'education-partners',
        loadComponent: () => import('./components/portal/staff/education/education-partner-management/education-partner-management.component').then(m => m.EducationPartnerManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Education Partners' }
      },
      {
        path: 'education-partners/new',
        loadComponent: () => import('./components/portal/staff/education/education-partner-edit/education-partner-edit.component').then(m => m.EducationPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Add Education Partner' }
      },
      {
        path: 'education-partners/:id/edit',
        loadComponent: () => import('./components/portal/staff/education/education-partner-edit/education-partner-edit.component').then(m => m.EducationPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Edit Education Partner' }
      },
      {
        path: 'business-partners',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-management/business-partner-management.component').then(m => m.BusinessPartnerManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Business Partners' }
      },
      {
        path: 'business-partners/new',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-edit/business-partner-edit.component').then(m => m.BusinessPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Add Business Partner' }
      },
      {
        path: 'financial-records',
        loadComponent: () => import('./components/portal/staff/financial/financial-record-management/financial-record-management.component').then(m => m.FinancialRecordManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Commission Records' }
      },
      {
        // Action Queue is now the default tab on the Accounts page rather than its own
        // screen — redirect so old links/bookmarks still land somewhere sensible.
        path: 'finance/queue',
        redirectTo: 'finance/accounts',
        pathMatch: 'full'
      },
      {
        path: 'finance/accounts',
        loadComponent: () => import('./components/portal/staff/finance/accounts/accounts.component').then(m => m.AccountsComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Accounts' }
      },
      {
        path: 'finance/accounts/timeline',
        loadComponent: () => import('./components/portal/staff/finance/account-timeline/account-timeline.component').then(m => m.AccountTimelineComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Account Timeline' }
      },
      {
        path: 'financial-records/:id',
        loadComponent: () => import('./components/portal/staff/financial/financial-detail/financial-detail.component').then(m => m.FinancialDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Financial Record' }
      },
      {
        path: 'business-partners/:id/edit',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-edit/business-partner-edit.component').then(m => m.BusinessPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Edit Business Partner' }
      },
      {
        path: 'roles',
        loadComponent: () => import('./components/portal/staff/role-management/role-management.component').then(m => m.RoleManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Roles' }
      },
      {
        path: 'users',
        loadComponent: () => import('./components/portal/staff/user-management/user-management.component').then(m => m.UserManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Users' }
      },
      {
        path: 'departments',
        loadComponent: () => import('./components/portal/staff/department-management/department-management.component').then(m => m.DepartmentManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Departments' }
      },
      {
        // My Tasks — every role (assigner/assignee access is the same for everyone);
        // TaskManagementComponent itself checks TasksView and shows a permission error
        // if missing, same pattern as every other permission-gated-but-not-role-gated
        // page in this app.
        path: 'my-tasks',
        loadComponent: () => import('./components/portal/staff/tasks/task-management/task-management.component').then(m => m.TaskManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'My Tasks' }
      },
      {
        // All Tasks — Manager/Admin only (department-scoped server-side too, see
        // TaskItemService.SearchAllTasksAsync).
        path: 'tasks',
        loadComponent: () => import('./components/portal/staff/tasks/all-tasks-management/all-tasks-management.component').then(m => m.AllTasksManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'All Tasks' }
      },
      {
        // Must come before 'tasks/:id' — see the routing note further down this file.
        path: 'tasks/new',
        loadComponent: () => import('./components/portal/staff/tasks/task-new/task-new.component').then(m => m.TaskNewComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Task' }
      },
      {
        path: 'tasks/:id',
        loadComponent: () => import('./components/portal/staff/tasks/task-detail/task-detail.component').then(m => m.TaskDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Task Detail' }
      },
      {
        path: 'applications/:id',
        loadComponent: () => import('./components/portal/staff/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Application Detail' }
      },
      {
        path: 'students',
        loadComponent: () => import('./components/portal/staff/student-management/student-management.component').then(m => m.StudentManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Students' }
      },
      {
        path: 'students/new',
        loadComponent: () => import('./components/portal/staff/student-management/student-new/student-new.component').then(m => m.StudentNewComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Add Student' }
      },
      {
        path: 'students/:id/edit',
        loadComponent: () => import('./components/portal/staff/student-management/student-edit/student-edit.component').then(m => m.StudentEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Edit Student' }
      },
      {
        path: 'students/:id',
        loadComponent: () => import('./components/portal/staff/student-management/student-detail/student-detail.component').then(m => m.StudentDetailComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Student Details' }
      },
            {
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile' }
      },
      {
        path: 'settings',
        loadComponent: () => import('./components/portal/staff/settings-management/settings-management.component').then(m => m.SettingsManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Settings' }
      },
      {
        path: 'dynamic-forms',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-management/dynamic-form-management.component').then(m => m.DynamicFormManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Dynamic Forms' }
      },
      {
        path: 'dynamic-forms/new',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-edit/dynamic-form-edit.component').then(m => m.DynamicFormEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Form' }
      },
      {
        path: 'dynamic-forms/:id',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-edit/dynamic-form-edit.component').then(m => m.DynamicFormEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Form' }
      },
      {
        // Permission-key gated (via the component's own hasMigrationCasesPermission()
        // check), not role-name gated — same shape as the 'enrolments' route above, since
        // MigrationCasesView is seeded to Staff/Manager/Admin, not Admin-only.
        path: 'migration-cases',
        loadComponent: () => import('./components/portal/staff/migration-cases/migration-case-management/migration-case-management.component').then(m => m.MigrationCaseManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Migration Cases' }
      },
      {
        path: 'migration-cases/:id',
        loadComponent: () => import('./components/portal/staff/migration-cases/migration-case-detail/migration-case-detail.component').then(m => m.MigrationCaseDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Case Detail' }
      },
      {
        path: 'visa-process-templates',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-management/visa-process-template-management.component').then(m => m.VisaProcessTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'VISA Process Templates' }
      },
      {
        path: 'visa-process-templates/new',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-edit/visa-process-template-edit.component').then(m => m.VisaProcessTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Template' }
      },
      {
        path: 'visa-process-templates/:id',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-edit/visa-process-template-edit.component').then(m => m.VisaProcessTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Template' }
      },
      {
        path: 'practitioner-tags',
        loadComponent: () => import('./components/portal/staff/practitioner-tags/practitioner-tag-management/practitioner-tag-management.component').then(m => m.PractitionerTagManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Practitioner Tags' }
      },
      {
        path: 'email-templates',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-management/email-template-management.component').then(m => m.EmailTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Email Templates' }
      },
      {
        path: 'email-templates/new',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-edit/email-template-edit.component').then(m => m.EmailTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Email Template' }
      },
      {
        path: 'email-templates/:id',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-edit/email-template-edit.component').then(m => m.EmailTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Email Template' }
      },
      {
        path: 'invoice-templates',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-management/invoice-template-management.component').then(m => m.InvoiceTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Invoice Templates' }
      },
      {
        path: 'invoice-templates/ledger',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-ledger/invoice-ledger.component').then(m => m.InvoiceLedgerComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Sent Invoices' }
      },
      {
        path: 'invoice-templates/new',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-edit/invoice-template-edit.component').then(m => m.InvoiceTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Invoice Template' }
      },
      {
        // Must come before the ':id' catch-all below, or "send-custom" would be parsed
        // as a template id and route to the edit screen instead.
        path: 'invoice-templates/send-custom',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-custom-send/invoice-custom-send.component').then(m => m.InvoiceCustomSendComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Send Custom Invoice' }
      },
      {
        path: 'invoice-templates/:id',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-edit/invoice-template-edit.component').then(m => m.InvoiceTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Invoice Template' }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
