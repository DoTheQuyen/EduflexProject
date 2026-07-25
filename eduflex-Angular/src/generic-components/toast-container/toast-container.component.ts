import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../app/services/notification.service';
import { NotificationComponent } from '../notification/notification.component';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, NotificationComponent],
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.css']
})
export class ToastContainerComponent {
  constructor(public notificationService: NotificationService) {}
}
