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
        canActivate: [AuthGuard]
      },
      {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [AuthGuard]
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
      { path: 'applications', component: ApplicationComponent, canActivate: [AuthGuard] },
      { path: 'feedback', component: FeedbackManagementComponent, canActivate: [AuthGuard] },
      { path: 'course-promotions', component: CoursePromotionManagementComponent, canActivate: [AuthGuard] },
      { path: 'roles', component: RoleManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'] } },
      { path: 'users', component: UserManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'] } },
      { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
      { path: '', redirectTo: 'applications', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
