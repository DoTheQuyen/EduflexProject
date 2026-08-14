import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Client, PermissionKey, UserFilterDto, UserSummaryDto } from '@services/api.services';
import { AuthHelperService, TaskPermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { TaskService } from '@services/task.service';
import { MigrationCaseService } from '@services/migration-case.service';
import { RecordPickerComponent } from '@generic/record-picker/record-picker.component';
import { CreateTaskRequest, LinkedRecordType } from '../../../../../models/task';
import { buildLinkPickerConfigs, LinkPickerConfig, resolveLinkedRecordLabel } from '../task-link-pickers';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

interface LinkedRecordState {
  id: string;
  label: string;
}

const LINK_TYPES: LinkedRecordType[] = ['Enrolment', 'Enquiry', 'Application', 'FinancialRecord', 'MigrationCase'];

@Component({
  selector: 'app-task-new',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RecordPickerComponent],
  templateUrl: './task-new.component.html',
  styleUrls: ['./task-new.component.css']
})
export class TaskNewComponent implements OnInit {
  permissions: TaskPermissions;
  staffOptions: UserSummaryDto[] = [];

  taskForm: FormGroup;
  isSaving = false;
  errorMessage = '';

  linkTypes = LINK_TYPES;
  linkConfigs: Record<LinkedRecordType, LinkPickerConfig>;
  selectedLinks: Partial<Record<LinkedRecordType, LinkedRecordState>> = {};
  lockedLinkType: LinkedRecordType | null = null;
  activePickerType: LinkedRecordType | null = null;

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private taskService: TaskService,
    private migrationCaseService: MigrationCaseService,
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasTasksPermission();
    this.linkConfigs = buildLinkPickerConfigs(this.apiClient, this.migrationCaseService);

    this.taskForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(2000)]],
      assigneeUserId: ['', [Validators.required]],
      dueDateTime: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    if (!this.permissions.add) {
      this.notificationService.error('You do not have permission to create tasks.');
      return;
    }

    this.apiClient.searchUsers(new UserFilterDto({ pageNumber: 1, pageSize: 500, isActive: true })).subscribe({
      next: (result) => {
        this.staffOptions = (result.items ?? []).filter((u) => u.roleName !== 'Student');
      },
      error: () => {}
    });

    // Opened from a record's Tasks tab (e.g. "New Task" on an Enrolment detail page) —
    // prefill and lock that one link so it can't be accidentally cleared/changed here.
    const params = this.route.snapshot.queryParamMap;
    for (const type of LINK_TYPES) {
      const field = type.charAt(0).toLowerCase() + type.slice(1) + 'Id'; // enrolmentId, enquiryId, applicationId, financialRecordId
      const id = params.get(field);
      if (id) {
        this.lockedLinkType = type;
        this.selectedLinks[type] = { id, label: id };
        resolveLinkedRecordLabel(this.apiClient, type, id, this.migrationCaseService).subscribe((label) => {
          this.selectedLinks[type] = { id, label };
        });
        break; // only one link is ever pre-set from a tab's "add" button
      }
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

  openPicker(type: LinkedRecordType): void {
    if (this.lockedLinkType === type) return;
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
    if (this.lockedLinkType === type) return;
    delete this.selectedLinks[type];
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.taskForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  onSubmit(): void {
    if (!this.permissions.add) {
      this.notificationService.error('You do not have permission to create tasks.');
      return;
    }

    this.taskForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.taskForm.invalid) {
      this.errorMessage = 'Please fix the highlighted fields before saving.';
      return;
    }

    this.isSaving = true;
    const formValue = this.taskForm.value;

    const payload: CreateTaskRequest = {
      name: formValue.name,
      description: formValue.description || undefined,
      assigneeUserId: formValue.assigneeUserId,
      dueDateTime: new Date(formValue.dueDateTime).toISOString(),
      enrolmentId: this.selectedLinks['Enrolment']?.id,
      enquiryId: this.selectedLinks['Enquiry']?.id,
      applicationId: this.selectedLinks['Application']?.id,
      financialRecordId: this.selectedLinks['FinancialRecord']?.id,
      migrationCaseId: this.selectedLinks['MigrationCase']?.id
    };

    this.taskService.create(payload).subscribe({
      next: (result) => {
        this.isSaving = false;
        this.notificationService.success('Task created successfully.');
        this.router.navigate(['/staff-portal/tasks', result.id], { replaceUrl: true });
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong creating the task. Please try again.');
      }
    });
  }

  cancel(): void {
    this.location.back();
  }
}
