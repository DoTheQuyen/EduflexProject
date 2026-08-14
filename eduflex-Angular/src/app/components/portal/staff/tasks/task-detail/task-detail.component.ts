import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Client, PermissionKey, UserFilterDto, UserSummaryDto } from '@services/api.services';
import { AuthHelperService, TaskPermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { TaskService } from '@services/task.service';
import { MigrationCaseService } from '@services/migration-case.service';
import { UserDirectoryService } from '@services/user-directory.service';
import { RecordPickerComponent } from '@generic/record-picker/record-picker.component';
import { ModalComponent } from '@generic/modal/modal.component';
import { LinkedRecordType, Task, TaskNote, TASK_STATUS_LABELS, taskStatusBadgeClass, UpdateTaskRequest } from '../../../../../models/task';
import { buildLinkPickerConfigs, LinkPickerConfig, resolveLinkedRecordLabel } from '../task-link-pickers';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

interface LinkedRecordState {
  id: string;
  label: string;
}

const LINK_TYPES: LinkedRecordType[] = ['Enrolment', 'Enquiry', 'Application', 'FinancialRecord', 'MigrationCase'];

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, RecordPickerComponent, ModalComponent],
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css']
})
export class TaskDetailComponent implements OnInit {
  taskId!: string;
  task: Task | null = null;
  isLoading = false;
  errorMessage = '';

  permissions: TaskPermissions;
  currentUserId = '';
  staffOptions: UserSummaryDto[] = [];

  isAssigner = false;
  isAssignee = false;

  mode: 'view' | 'edit' = 'view';
  editForm: FormGroup;
  isSaving = false;

  showReassignModal = false;
  reassignForm: FormGroup;
  isReassigning = false;

  noteContent = '';
  isAddingNote = false;
  isChangingStatus = false;

  linkTypes = LINK_TYPES;
  linkConfigs: Record<LinkedRecordType, LinkPickerConfig>;
  selectedLinks: Partial<Record<LinkedRecordType, LinkedRecordState>> = {};
  activePickerType: LinkedRecordType | null = null;

  readonly statusLabels = TASK_STATUS_LABELS;
  readonly statusBadgeClass = taskStatusBadgeClass;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private taskService: TaskService,
    private migrationCaseService: MigrationCaseService,
    public userDirectory: UserDirectoryService,
    private route: ActivatedRoute,
    private location: Location,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasTasksPermission();
    this.currentUserId = this.authHelper.getCurrentUser()?.id ?? '';
    this.linkConfigs = buildLinkPickerConfigs(this.apiClient, this.migrationCaseService);

    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(2000)]],
      dueDateTime: ['', [Validators.required]]
    });

    this.reassignForm = this.fb.group({
      newAssigneeUserId: ['', [Validators.required]],
      note: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id') ?? '';
    this.userDirectory.load().subscribe();
    this.loadStaff();
    this.loadTask();
  }

  get canEditDetails(): boolean {
    return this.permissions.edit && this.isAssigner && this.task?.status !== 'Completed';
  }

  get canRespond(): boolean {
    return this.permissions.view && (this.isAssigner || this.isAssignee);
  }

  // Newest first. A plain getter (not an inline template expression) so the compiler
  // has a clean `Task` (not `Task | null`) to narrow against — spreading `task.notes`
  // directly inside *ngFor's microsyntax lost that narrowing and made `note` look
  // possibly-undefined to strictTemplates. Also avoids re-spreading/reversing a new
  // array on every change-detection pass.
  get reversedNotes(): TaskNote[] {
    return this.task ? [...this.task.notes].reverse() : [];
  }

  loadStaff(): void {
    this.apiClient.searchUsers(new UserFilterDto({ pageNumber: 1, pageSize: 500, isActive: true })).subscribe({
      next: (result) => {
        this.staffOptions = (result.items ?? []).filter((u) => u.roleName !== 'Student' && u.id !== this.task?.assigneeUserId);
      },
      error: () => {}
    });
  }

  loadTask(): void {
    this.isLoading = true;
    this.taskService.getById(this.taskId).subscribe({
      next: (task) => {
        this.task = task;
        this.isAssigner = task.assignerUserId === this.currentUserId;
        this.isAssignee = task.assigneeUserId === this.currentUserId;
        this.applyTaskToEditForm(task);
        this.loadLinkedLabels(task);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this task.');
      }
    });
  }

  private applyTaskToEditForm(task: Task): void {
    this.editForm.patchValue({
      name: task.name,
      description: task.description ?? '',
      dueDateTime: task.dueDateTime ? this.toDateTimeInputValue(task.dueDateTime) : ''
    });
  }

  private toDateTimeInputValue(value: string): string {
    const d = new Date(value);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private loadLinkedLabels(task: Task): void {
    this.selectedLinks = {};
    const links: [LinkedRecordType, string | undefined][] = [
      ['Enrolment', task.enrolmentId],
      ['Enquiry', task.enquiryId],
      ['Application', task.applicationId],
      ['FinancialRecord', task.financialRecordId],
      ['MigrationCase', task.migrationCaseId]
    ];
    for (const [type, id] of links) {
      if (!id) continue;
      this.selectedLinks[type] = { id, label: id };
      resolveLinkedRecordLabel(this.apiClient, type, id, this.migrationCaseService).subscribe((label) => {
        this.selectedLinks[type] = { id, label };
      });
    }
  }

  canLink(type: LinkedRecordType): boolean {
    switch (type) {
      case 'Enrolment': return this.authHelper.hasEnrolmentsPermission().view;
      case 'Enquiry': return this.authHelper.hasEnquiryPermission().view;
      case 'Application': return this.authHelper.hasPermission(PermissionKey.ApplicationsView);
      case 'FinancialRecord': return this.authHelper.hasFinancePermission().view;
      case 'MigrationCase': return this.authHelper.hasMigrationCasesPermission().view;
    }
  }

  enterEditMode(): void {
    if (!this.canEditDetails) {
      this.notificationService.error('Only this task\'s assigner can edit its details, and only while it is not completed.');
      return;
    }
    this.errorMessage = '';
    this.mode = 'edit';
  }

  cancelEdit(): void {
    this.mode = 'view';
    this.errorMessage = '';
    if (this.task) {
      this.applyTaskToEditForm(this.task);
      this.loadLinkedLabels(this.task);
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.editForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  openPicker(type: LinkedRecordType): void {
    if (this.mode !== 'edit') return;
    this.activePickerType = type;
  }

  onPicked(row: any): void {
    if (!this.activePickerType) return;
    const config = this.linkConfigs[this.activePickerType];
    this.selectedLinks[this.activePickerType] = { id: row.id, label: config.label(row) };
    this.activePickerType = null;
  }

  onPickerClosed(): void {
    this.activePickerType = null;
  }

  removeLink(type: LinkedRecordType): void {
    if (this.mode !== 'edit') return;
    delete this.selectedLinks[type];
  }

  saveEdit(): void {
    if (!this.task) return;

    this.editForm.markAllAsTouched();
    this.errorMessage = '';
    if (this.editForm.invalid) {
      this.errorMessage = 'Please fix the highlighted fields before saving.';
      return;
    }

    const formValue = this.editForm.value;
    const payload: UpdateTaskRequest = {
      name: formValue.name,
      description: formValue.description || undefined,
      dueDateTime: new Date(formValue.dueDateTime).toISOString(),
      enrolmentId: this.selectedLinks['Enrolment']?.id,
      enquiryId: this.selectedLinks['Enquiry']?.id,
      applicationId: this.selectedLinks['Application']?.id,
      financialRecordId: this.selectedLinks['FinancialRecord']?.id,
      migrationCaseId: this.selectedLinks['MigrationCase']?.id
    };

    this.isSaving = true;
    this.taskService.update(this.taskId, payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.mode = 'view';
        this.notificationService.success('Task updated successfully.');
        this.loadTask();
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the task. Please try again.');
      }
    });
  }

  addNote(): void {
    if (!this.noteContent.trim()) return;
    this.isAddingNote = true;
    this.taskService.addNote(this.taskId, this.noteContent.trim()).subscribe({
      next: (task) => {
        this.task = task;
        this.noteContent = '';
        this.isAddingNote = false;
      },
      error: (err) => {
        this.isAddingNote = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not add note. Please try again.'));
      }
    });
  }

  openReassignModal(): void {
    this.reassignForm.reset();
    this.showReassignModal = true;
  }

  confirmReassign(): void {
    this.reassignForm.markAllAsTouched();
    if (this.reassignForm.invalid) return;

    this.isReassigning = true;
    const { newAssigneeUserId, note } = this.reassignForm.value;
    this.taskService.reassign(this.taskId, { newAssigneeUserId, note }).subscribe({
      next: () => {
        this.isReassigning = false;
        this.showReassignModal = false;
        this.notificationService.success('Task reassigned.');
        this.loadTask();
      },
      error: (err) => {
        this.isReassigning = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not reassign this task. Please try again.'));
      }
    });
  }

  markComplete(): void {
    this.isChangingStatus = true;
    this.taskService.changeStatus(this.taskId, 'Completed').subscribe({
      next: () => {
        this.isChangingStatus = false;
        this.notificationService.success('Task marked complete.');
        this.loadTask();
      },
      error: (err) => {
        this.isChangingStatus = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not complete this task. Please try again.'));
      }
    });
  }

  reopenTask(): void {
    this.isChangingStatus = true;
    this.taskService.changeStatus(this.taskId, 'Processing').subscribe({
      next: () => {
        this.isChangingStatus = false;
        this.notificationService.success('Task reopened.');
        this.loadTask();
      },
      error: (err) => {
        this.isChangingStatus = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not reopen this task. Please try again.'));
      }
    });
  }

  // Task detail is reachable from My Tasks, All Tasks, or a linked-record's Tasks tab —
  // browser history back (not a hardcoded route) is the only thing that lands back on
  // whichever of those the user actually came from.
  back(): void {
    this.location.back();
  }

  linkedRecordRoute(type: LinkedRecordType, id: string): string[] {
    switch (type) {
      case 'Enrolment': return ['/staff-portal/enrolments', id];
      case 'Enquiry': return ['/staff-portal/enquiries', id];
      case 'Application': return ['/staff-portal/applications', id];
      case 'FinancialRecord': return ['/staff-portal/financial-records', id];
      case 'MigrationCase': return ['/staff-portal/migration-cases', id];
    }
  }
}
