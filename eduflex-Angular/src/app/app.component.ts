import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/public/navbar/navbar.component';
import { FooterComponent } from './components/public/footer/footer.component';
import { Router } from '@angular/router';
import { LanguageService } from './services/language.service';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Toast } from 'primeng/toast';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, FooterComponent, ConfirmDialog, Toast],
  templateUrl: './app.component.html',
})
export class AppComponent {
  title = 'Eduflex';

  constructor(private router: Router) {}

  isPortalRoute(): boolean {
    return (
      this.router.url.startsWith('/student-portal') || this.router.url.startsWith('/staff-portal')
    );
  }
}
