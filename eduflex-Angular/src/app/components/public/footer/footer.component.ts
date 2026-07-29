import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

interface FooterSocialLink {
  label: string;
  icon: string;
  url: string;
}

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.css']
})
export class FooterComponent {
  currentYear: number = new Date().getFullYear();

  socialLinks: FooterSocialLink[] = [
    { label: 'Facebook', icon: 'fab fa-facebook-f', url: '' },
    { label: 'Instagram', icon: 'fab fa-instagram', url: '' },
    { label: 'LinkedIn', icon: 'fab fa-linkedin-in', url: '' },
    { label: 'YouTube', icon: 'fab fa-youtube', url: '' }
  ];
}