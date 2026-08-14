import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TaskListComponent } from '@generic/task-list/task-list.component';
import { AuthHelperService } from '@services/auth-helper.service';
import { FinancialRecord } from '../../../../../../../models/financial-record';

@Component({
  selector: 'app-financial-tasks-tab',
  standalone: true,
  imports: [CommonModule, TaskListComponent],
  templateUrl: './tasks-tab.component.html'
})
export class FinancialTasksTabComponent {
  @Input({ required: true }) record!: FinancialRecord;

  canAdd: boolean;

  constructor(private router: Router, authHelper: AuthHelperService) {
    this.canAdd = authHelper.hasTasksPermission().add;
  }

  onViewTask(taskId: string): void {
    this.router.navigate(['/staff-portal/tasks', taskId]);
  }

  onAddTask(): void {
    this.router.navigate(['/staff-portal/tasks/new'], { queryParams: { financialRecordId: this.record.id } });
  }
}
