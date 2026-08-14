import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthHelperService, TaskPermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { TaskListComponent } from '@generic/task-list/task-list.component';

// My Tasks — every staff member's own tasks, whether they're the assigner or the
// assignee. Thin host around the generic task-list; all the actual list/search/tab
// logic lives there so this page (and All Tasks below) don't duplicate it.
@Component({
  selector: 'app-task-management',
  standalone: true,
  imports: [CommonModule, TaskListComponent],
  templateUrl: './task-management.component.html',
  styleUrls: ['./task-management.component.css']
})
export class TaskManagementComponent {
  permissions: TaskPermissions;

  constructor(
    private router: Router,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasTasksPermission();
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view tasks.');
    }
  }

  // Task detail/new are shared routes under /staff-portal/tasks — reached the same way
  // whether opened from My Tasks, All Tasks, or a linked-record Tasks tab.
  onViewTask(taskId: string): void {
    this.router.navigate(['/staff-portal/tasks', taskId]);
  }

  onAddTask(): void {
    this.router.navigate(['/staff-portal/tasks/new']);
  }
}
