import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { AboutComponent } from './components/about/about.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { HomepageComponent } from './components/activities/home/homepage.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'about', component: AboutComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register',  loadComponent: () =>
      import('./components/register/register.component').then(m => m.RegisterComponent) },
  {
    path: '',
    component: HomepageComponent,
    canActivate: [AuthGuard],
    children: [
      // { path: 'dashboard', loadChildren: () => import('./modules/dashboard/dashboard.module').then(m => m.DashboardModule) },
      // { path: 'courses', loadChildren: () => import('./modules/courses/courses.module').then(m => m.CoursesModule) },
      // { path: 'students', loadChildren: () => import('./modules/students/students.module').then(m => m.StudentsModule) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }