import { Routes } from '@angular/router';
import { HomeComponent } from './components/public/home/home.component';
import { AboutComponent } from './components/public/about/about.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { HomepageComponent } from './components/portal/home/homepage.component';
import { ApplicationComponent } from './components/portal/student/applications/application.component';
import { ProfileComponent } from './components/portal/share-component/profile/profile.component';
import { AuthGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';
import { FeedbackManagementComponent } from './components/portal/staff/feedback-management/feedback-management.component';
import { CoursePromotionManagementComponent } from './components/portal/staff/course-promotion-management/course-promotion-management.component';
import { RoleManagementComponent } from './components/portal/staff/role-management/role-management.component';
import { UserManagementComponent } from './components/portal/staff/user-management/user-management.component';
import { DashboardComponent } from './components/portal/staff/dashboard/dashboard.component';
import { StudentHandbookComponent } from './components/public/student-handbook/student-handbook.component';

export const routes: Routes = [
  { path: '', component: HomeComponent }, // Public home page
  { path: 'about', component: AboutComponent }, // Public about page
  { path: 'login', component: LoginComponent }, // Public login page
  { path: 'student-handbook', component: StudentHandbookComponent }, // Public student handbook page
  { 
    path: 'register',  
    loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent) 
  },

  // Student portal (parent)
  {
    path: 'student-portal',
    component: HomepageComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'application',
        component: ApplicationComponent,
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Application' }
      },
      {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [AuthGuard],
        data: { breadcrumb: 'Profile' }
      },
      { path: '', redirectTo: 'application', pathMatch: 'full' } // default child
    ]
  },

    // Staff portal
  {
    path: 'staff-portal', 
    component: HomepageComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard], data: { breadcrumb: 'Dashboard' } },
      { path: 'applications', component: ApplicationComponent, canActivate: [AuthGuard], data: { breadcrumb: 'Applications' } },
      { path: 'feedback', component: FeedbackManagementComponent, canActivate: [AuthGuard], data: { breadcrumb: 'Feedback' } },
      { path: 'course-promotions', component: CoursePromotionManagementComponent, canActivate: [AuthGuard], data: { breadcrumb: 'Course Promotions' } },
      { path: 'roles', component: RoleManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'], breadcrumb: 'Roles' } },
      { path: 'users', component: UserManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'], breadcrumb: 'Users' } },
      { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard], data: { breadcrumb: 'Profile' } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];