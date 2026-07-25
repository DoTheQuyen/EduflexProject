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
        path: 'application',
        loadComponent: () => import('./components/portal/student/applications/application.component').then(m => m.ApplicationComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application' }
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile' }
      },
      { path: '', redirectTo: 'application', pathMatch: 'full' } // default child
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
        path: 'profile',
        loadComponent: () => import('./components/portal/share-component/profile/profile.component').then(m => m.ProfileComponent),
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile' }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
