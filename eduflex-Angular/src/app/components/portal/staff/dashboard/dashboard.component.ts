import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthHelperService } from '../../../../services/auth-helper.service';

interface DashboardCard {
  title: string;
  description: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  userInfo: any;
  today = new Date();
  cards: DashboardCard[] = [];

  constructor(private authHelper: AuthHelperService) {}

  ngOnInit(): void {
    this.userInfo = this.authHelper.getCurrentUser();
    this.setCards();
  }

  private setCards(): void {
    const allCards: DashboardCard[] = [
      { title: 'Applications', description: 'Review student applications', icon: 'clipboard-list', route: '/staff-portal/applications' },
      { title: 'Feedback', description: 'Read student feedback', icon: 'comment', route: '/staff-portal/feedback' },
      { title: 'Course Promotions', description: 'Manage promotional campaigns', icon: 'bullhorn', route: '/staff-portal/course-promotions' },
      { title: 'Roles', description: 'Configure role permissions', icon: 'lock', route: '/staff-portal/roles' },
      { title: 'Users', description: 'Manage staff and student accounts', icon: 'users', route: '/staff-portal/users' }
    ];

    this.cards = this.userInfo?.role === 'Admin'
      ? allCards
      : allCards.filter(c => c.title !== 'Roles' && c.title !== 'Users');
  }
}