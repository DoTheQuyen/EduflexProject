import { Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private messageService: MessageService) {}

  success(message: string): void {
    this.messageService.add({ severity: 'success', summary: 'Success', detail: message, life: 4000 });
  }

  error(message: string): void {
    this.messageService.add({ severity: 'error', summary: 'Error', detail: message, life: 4000 });
  }
}
