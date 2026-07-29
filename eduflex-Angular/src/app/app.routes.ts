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
        data: { breadcrumb: 'Finance' }
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
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
