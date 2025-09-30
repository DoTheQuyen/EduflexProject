import { Routes } from '@angular/router';
import { HomeComponent } from './components/public/home/home.component';
import { AboutComponent } from './components/public/about/about.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { HomepageComponent } from './components/portal/home/homepage.component';
import { ApplicationComponent } from './components/portal/student/applications/application.component';
import { ProfileComponent } from './components/portal/share-component/profile/profile.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent }, // Public home page
  { path: 'about', component: AboutComponent }, // Public about page
  { path: 'login', component: LoginComponent }, // Public login page
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
      {
        path: 'applications',
        component: ApplicationComponent,
        canActivate: [AuthGuard]
      },
      {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [AuthGuard]
      },
      { path: '', redirectTo: 'applications', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' } // Fallback to public home
];
