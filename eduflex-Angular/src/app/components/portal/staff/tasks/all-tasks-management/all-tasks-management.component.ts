import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthHelperService, TaskPermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { TaskListComponent } from '@generic/task-list/task-list.component';

// All Tasks — Manager/Admin only, scoped server-side to the department(s) the current
// user heads (TaskItemService.SearchAllTasksAsync). Route itself is also role-gated
// (see app.routes.ts) so a plain Staff member never even reaches this page, but the
// permissions.viewAll check here still gates rendering in case that ever drifts.
@Component({
  selector: 'app-all-tasks-management',
  standalone: true,
  imports: [CommonModule, TaskListComponent],
  templateUrl: './all-tasks-management.component.html',
  styleUrls: ['./all-tasks-management.component.css']
})
export class AllTasksManagementComponent {
  permissions: TaskPermissions;

  constructor(
    private router: Router,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasTasksPermission();
    if (!this.permissions.viewAll) {
      this.notificationService.error('You do not have permission to view all department tasks.');
    }
  }

  onViewTask(taskId: string): void {
    this.router.navigate(['/staff-portal/tasks', taskId]);
  }

  onAddTask(): void {
    this.router.navigate(['/staff-portal/tasks/new']);
  }
}
