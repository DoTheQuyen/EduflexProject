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
    path: 'feedback',
    loadComponent: () => import('./components/public/feedback/feedback.component').then(m => m.FeedbackComponent)
  }, // Public student feedback wall
  {
    path: 'plan-ahead',
    loadComponent: () => import('./components/public/plan-ahead/plan-ahead.component').then(m => m.PlanAheadComponent)
  }, // Public general timing/planning advice page
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
        data: { breadcrumb: 'Dashboard', helpKey: 'user/dashboard' }
      },
      {
        path: 'application',
        loadComponent: () => import('./components/portal/student/applications/application.component').then(m => m.ApplicationComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application', helpKey: 'user/applications/track-application-status' }
      },
      {
        path: 'application/new',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Application', helpKey: 'user/applications/create-an-application' }
      },
      {
        path: 'application/:id',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application Detail', helpKey: 'user/applications/track-application-status' }
      },
            {
        path: 'application/:id/forms/:formId',
        loadComponent: () => import('./components/portal/student/applications/application-form-fill/application-form-fill.component').then(m => m.ApplicationFormFillComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Fill Form', helpKey: 'user/forms/submit-a-form-request' }
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile', helpKey: 'user/profile/complete-your-profile' }
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
        data: { breadcrumb: 'Dashboard', helpKey: 'staff/dashboard' }
      },
      {
        path: 'applications',
        loadComponent: () => import('./components/portal/student/applications/application.component').then(m => m.ApplicationComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Applications', helpKey: 'staff/applications/review-an-application' }
      },
      {
        path: 'applications/:id',
        loadComponent: () => import('./components/portal/student/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application Detail', helpKey: 'staff/applications/review-an-application' }
      },
      {
        path: 'feedback',
        loadComponent: () => import('./components/portal/staff/feedback-management/feedback-management.component').then(m => m.FeedbackManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Feedback', helpKey: 'staff/marketing/moderate-feedback' }
      },
      {
        path: 'course-promotions',
        loadComponent: () => import('./components/portal/staff/course-promotion-management/course-promotion-management.component').then(m => m.CoursePromotionManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Course Promotions', helpKey: 'staff/marketing/manage-course-promotions' }
      },
      {
        path: 'enquiries',
        loadComponent: () => import('./components/portal/staff/enquiries/enquiry-management/enquiry-management.component').then(m => m.EnquiryManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enquiries', helpKey: 'staff/enquiries/manage-enquiries' }
      },
      {
        path: 'enquiries/:id',
        loadComponent: () => import('./components/portal/staff/enquiries/enquiry-details/enquiry-detail.component').then(m => m.EnquiryDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enquiry Detail', helpKey: 'staff/enquiries/manage-enquiries' }
      },
      {
        path: 'enrolments',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-management/enrolment-management.component').then(m => m.EnrolmentManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enrolments', helpKey: 'staff/enrolments/create-an-enrolment' }
      },
      {
        path: 'enrolments/new',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-new/enrolment-new.component').then(m => m.EnrolmentNewComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Enrolment', helpKey: 'staff/enrolments/create-an-enrolment' }
      },
      {
        path: 'enrolments/:id',
        loadComponent: () => import('./components/portal/staff/enrolments/enrolment-detail/enrolment-detail.component').then(m => m.EnrolmentDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Enrolment Detail', helpKey: 'staff/enrolments/create-an-enrolment' }
      },
      {
        path: 'education-partners',
        loadComponent: () => import('./components/portal/staff/education/education-partner-management/education-partner-management.component').then(m => m.EducationPartnerManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Education Partners', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'education-partners/new',
        loadComponent: () => import('./components/portal/staff/education/education-partner-edit/education-partner-edit.component').then(m => m.EducationPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Add Education Partner', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'education-partners/:id/edit',
        loadComponent: () => import('./components/portal/staff/education/education-partner-edit/education-partner-edit.component').then(m => m.EducationPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Edit Education Partner', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'business-partners',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-management/business-partner-management.component').then(m => m.BusinessPartnerManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Business Partners', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'business-partners/new',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-edit/business-partner-edit.component').then(m => m.BusinessPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Add Business Partner', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'financial-records',
        loadComponent: () => import('./components/portal/staff/financial/financial-record-management/financial-record-management.component').then(m => m.FinancialRecordManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Commission Records', helpKey: 'staff/finance/record-a-commission' }
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
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Accounts', helpKey: 'staff/finance/accounts' }
      },
      {
        path: 'finance/accounts/timeline',
        loadComponent: () => import('./components/portal/staff/finance/account-timeline/account-timeline.component').then(m => m.AccountTimelineComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Account Timeline', helpKey: 'staff/finance/accounts' }
      },
      {
        path: 'financial-records/:id',
        loadComponent: () => import('./components/portal/staff/financial/financial-detail/financial-detail.component').then(m => m.FinancialDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Financial Record', helpKey: 'staff/finance/record-a-commission' }
      },
      {
        path: 'business-partners/:id/edit',
        loadComponent: () => import('./components/portal/staff/business-partners/business-partner-edit/business-partner-edit.component').then(m => m.BusinessPartnerEditComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Edit Business Partner', helpKey: 'staff/partners/manage-partners' }
      },
      {
        path: 'roles',
        loadComponent: () => import('./components/portal/staff/role-management/role-management.component').then(m => m.RoleManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Roles', helpKey: 'admin/roles/create-a-role' }
      },
      {
        path: 'users',
        loadComponent: () => import('./components/portal/staff/user-management/user-management.component').then(m => m.UserManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Users', helpKey: 'admin/users/add-a-user' }
      },
      {
        path: 'departments',
        loadComponent: () => import('./components/portal/staff/department-management/department-management.component').then(m => m.DepartmentManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'Departments', helpKey: 'admin/departments/manage-departments' }
      },
      {
        // My Tasks — every role (assigner/assignee access is the same for everyone);
        // TaskManagementComponent itself checks TasksView and shows a permission error
        // if missing, same pattern as every other permission-gated-but-not-role-gated
        // page in this app.
        path: 'my-tasks',
        loadComponent: () => import('./components/portal/staff/tasks/task-management/task-management.component').then(m => m.TaskManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'My Tasks', helpKey: 'staff/tasks/manage-tasks' }
      },
      {
        // All Tasks — Manager/Admin only (department-scoped server-side too, see
        // TaskItemService.SearchAllTasksAsync).
        path: 'tasks',
        loadComponent: () => import('./components/portal/staff/tasks/all-tasks-management/all-tasks-management.component').then(m => m.AllTasksManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager'], breadcrumb: 'All Tasks', helpKey: 'staff/tasks/manage-tasks' }
      },
      {
        // Must come before 'tasks/:id' — see the routing note further down this file.
        path: 'tasks/new',
        loadComponent: () => import('./components/portal/staff/tasks/task-new/task-new.component').then(m => m.TaskNewComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'New Task', helpKey: 'staff/tasks/manage-tasks' }
      },
      {
        path: 'tasks/:id',
        loadComponent: () => import('./components/portal/staff/tasks/task-detail/task-detail.component').then(m => m.TaskDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Task Detail', helpKey: 'staff/tasks/manage-tasks' }
      },
      {
        path: 'applications/:id',
        loadComponent: () => import('./components/portal/staff/applications/application-detail/application-detail.component').then(m => m.ApplicationDetailComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Application Detail', helpKey: 'staff/applications/review-an-application' }
      },
      {
        path: 'students',
        loadComponent: () => import('./components/portal/staff/student-management/student-management.component').then(m => m.StudentManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Students', helpKey: 'staff/students/add-a-student' }
      },
      {
        path: 'students/new',
        loadComponent: () => import('./components/portal/staff/student-management/student-new/student-new.component').then(m => m.StudentNewComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Add Student', helpKey: 'staff/students/add-a-student' }
      },
      {
        path: 'students/:id/edit',
        loadComponent: () => import('./components/portal/staff/student-management/student-edit/student-edit.component').then(m => m.StudentEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Edit Student', helpKey: 'staff/students/add-a-student' }
      },
      {
        path: 'students/:id',
        loadComponent: () => import('./components/portal/staff/student-management/student-detail/student-detail.component').then(m => m.StudentDetailComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Manager', 'Staff'], breadcrumb: 'Student Details', helpKey: 'staff/students/view-a-student' }
      },
            {
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile', helpKey: 'user/profile/complete-your-profile' }
      },
      {
        path: 'settings',
        loadComponent: () => import('./components/portal/staff/settings-management/settings-management.component').then(m => m.SettingsManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Settings', helpKey: 'admin/settings/app-settings' }
      },
      {
        path: 'dynamic-forms',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-management/dynamic-form-management.component').then(m => m.DynamicFormManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Dynamic Forms', helpKey: 'admin/templates/create-a-form-template' }
      },
      {
        path: 'dynamic-forms/new',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-edit/dynamic-form-edit.component').then(m => m.DynamicFormEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Form', helpKey: 'admin/templates/create-a-form-template' }
      },
      {
        path: 'dynamic-forms/:id',
        loadComponent: () => import('./components/portal/staff/dynamic-forms/dynamic-form-edit/dynamic-form-edit.component').then(m => m.DynamicFormEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Form', helpKey: 'admin/templates/create-a-form-template' }
      },
      {
        // Permission-key gated (via the component's own hasMigrationCasesPermission()
        // check), not role-name gated — same shape as the 'enrolments' route above, since
        // MigrationCasesView is seeded to Staff/Manager/Admin, not Admin-only.
        path: 'migration-cases',
        loadComponent: () => import('./components/portal/staff/migration-cases/migration-case-management/migration-case-management.component').then(m => m.MigrationCaseManagementComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Migration Cases', helpKey: 'staff/migration-cases/work-a-migration-case' }
      },
      {
        path: 'migration-cases/:id',
        loadComponent: () => import('./components/portal/staff/migration-cases/migration-case-detail/migration-case-detail.component').then(m => m.MigrationCaseDetailComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Case Detail', helpKey: 'staff/migration-cases/work-a-migration-case' }
      },
      {
        path: 'visa-process-templates',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-management/visa-process-template-management.component').then(m => m.VisaProcessTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'VISA Process Templates', helpKey: 'admin/templates/configure-a-visa-process-template' }
      },
      {
        path: 'visa-process-templates/new',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-edit/visa-process-template-edit.component').then(m => m.VisaProcessTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Template', helpKey: 'admin/templates/configure-a-visa-process-template' }
      },
      {
        path: 'visa-process-templates/:id',
        loadComponent: () => import('./components/portal/staff/visa-process-templates/visa-process-template-edit/visa-process-template-edit.component').then(m => m.VisaProcessTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Template', helpKey: 'admin/templates/configure-a-visa-process-template' }
      },
      {
        path: 'practitioner-tags',
        loadComponent: () => import('./components/portal/staff/practitioner-tags/practitioner-tag-management/practitioner-tag-management.component').then(m => m.PractitionerTagManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Practitioner Tags', helpKey: 'admin/templates/manage-practitioner-tags' }
      },
      {
        path: 'email-templates',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-management/email-template-management.component').then(m => m.EmailTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Email Templates', helpKey: 'admin/templates/manage-email-templates' }
      },
      {
        path: 'email-templates/new',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-edit/email-template-edit.component').then(m => m.EmailTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Email Template', helpKey: 'admin/templates/manage-email-templates' }
      },
      {
        path: 'email-templates/:id',
        loadComponent: () => import('./components/portal/staff/email-templates/email-template-edit/email-template-edit.component').then(m => m.EmailTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Email Template', helpKey: 'admin/templates/manage-email-templates' }
      },
      {
        path: 'invoice-templates',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-management/invoice-template-management.component').then(m => m.InvoiceTemplateManagementComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Invoice Templates', helpKey: 'admin/templates/manage-invoice-templates' }
      },
      {
        path: 'invoice-templates/ledger',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-ledger/invoice-ledger.component').then(m => m.InvoiceLedgerComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Sent Invoices', helpKey: 'staff/finance/send-a-custom-invoice' }
      },
      {
        path: 'invoice-templates/new',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-edit/invoice-template-edit.component').then(m => m.InvoiceTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'New Invoice Template', helpKey: 'admin/templates/manage-invoice-templates' }
      },
      {
        // Must come before the ':id' catch-all below, or "send-custom" would be parsed
        // as a template id and route to the edit screen instead.
        path: 'invoice-templates/send-custom',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-custom-send/invoice-custom-send.component').then(m => m.InvoiceCustomSendComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Send Custom Invoice', helpKey: 'staff/finance/send-a-custom-invoice' }
      },
      {
        path: 'invoice-templates/:id',
        loadComponent: () => import('./components/portal/staff/invoice-templates/invoice-template-edit/invoice-template-edit.component').then(m => m.InvoiceTemplateEditComponent),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin'], breadcrumb: 'Edit Invoice Template', helpKey: 'admin/templates/manage-invoice-templates' }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
