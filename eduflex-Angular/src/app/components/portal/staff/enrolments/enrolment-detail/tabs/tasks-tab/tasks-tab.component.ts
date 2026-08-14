import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TaskListComponent } from '@generic/task-list/task-list.component';
import { AuthHelperService } from '@services/auth-helper.service';
import { Enrolment } from '../../../../../../../models/enrolment';

@Component({
  selector: 'app-enrolment-tasks-tab',
  standalone: true,
  imports: [CommonModule, TaskListComponent],
  templateUrl: './tasks-tab.component.html'
})
export class EnrolmentTasksTabComponent {
  @Input({ required: true }) enrolment!: Enrolment;

  canAdd: boolean;

  constructor(private router: Router, authHelper: AuthHelperService) {
    this.canAdd = authHelper.hasTasksPermission().add;
  }

  onViewTask(taskId: string): void {
    this.router.navigate(['/staff-portal/tasks', taskId]);
  }

  // Prefills and locks the Enrolment link on the New Task form — see
  // TaskNewComponent.ngOnInit's queryParamMap read.
  onAddTask(): void {
    this.router.navigate(['/staff-portal/tasks/new'], { queryParams: { enrolmentId: this.enrolment.id } });
  }
}
