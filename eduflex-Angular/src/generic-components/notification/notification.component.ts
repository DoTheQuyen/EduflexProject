import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Message } from 'primeng/message';

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [CommonModule, Message],
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.css']
})
export class NotificationComponent {
  @Input() type: 'success' | 'error' | 'warning' | 'info' = 'info';
  @Input() message = '';
  @Input() dismissible = false;
  @Output() dismissed = new EventEmitter<void>();

  get severity(): 'success' | 'error' | 'warn' | 'info' {
    return this.type === 'warning' ? 'warn' : this.type;
  }
}
