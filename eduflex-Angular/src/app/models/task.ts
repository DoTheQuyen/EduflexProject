export type TaskStatus = 'New' | 'Processing' | 'Completed';

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  New: 'New',
  Processing: 'Processing',
  Completed: 'Completed'
};

export function taskStatusBadgeClass(status: TaskStatus): string {
  switch (status) {
    case 'Completed': return 'badge-pill-success-soft';
    case 'Processing': return 'badge-pill-accent-soft';
    default: return 'badge-pill-muted-soft'; // New
  }
}

export type TaskNoteType = 'Note' | 'StatusChange' | 'Reassign';

export interface TaskNote {
  id: string;
  type: TaskNoteType;
  content: string;
  createdByUserId?: string;
  createdByName: string;
  createdAt: string;
}

export interface Task {
  id: string;
  name: string;
  description?: string;
  assignerUserId: string;
  assigneeUserId: string;
  dueDateTime: string;
  status: TaskStatus;
  enrolmentId?: string;
  enquiryId?: string;
  applicationId?: string;
  financialRecordId?: string;
  migrationCaseId?: string;
  notes: TaskNote[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  name: string;
  description?: string;
  assigneeUserId: string;
  dueDateTime: string;
  enrolmentId?: string;
  enquiryId?: string;
  applicationId?: string;
  financialRecordId?: string;
  migrationCaseId?: string;
}

// No assigneeUserId/status — reassigning and completing/reopening go through their own
// dedicated endpoints (each records its own mandatory timeline note), never a plain edit.
export interface UpdateTaskRequest {
  name: string;
  description?: string;
  dueDateTime: string;
  enrolmentId?: string;
  enquiryId?: string;
  applicationId?: string;
  financialRecordId?: string;
  migrationCaseId?: string;
}

export interface TaskFilter {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  status?: TaskStatus;
  // Set to 'Completed' for the Active tab (New + Processing together) — see the
  // backend's matching TaskItemFilter.ExcludeStatus doc comment for why this isn't
  // just a second value on `status`.
  excludeStatus?: TaskStatus;
  enrolmentId?: string;
  enquiryId?: string;
  applicationId?: string;
  financialRecordId?: string;
  migrationCaseId?: string;
}

export interface ReassignTaskRequest {
  newAssigneeUserId: string;
  note: string;
}

// The record types a task can optionally link to — drives both the generic record-picker
// component and the "which linked-id field am I filtering by" logic in the generic
// task-list component's linked-record mode.
export type LinkedRecordType = 'Enrolment' | 'Enquiry' | 'Application' | 'FinancialRecord' | 'MigrationCase';

export const LINKED_RECORD_FILTER_FIELD: Record<LinkedRecordType, keyof TaskFilter> = {
  Enrolment: 'enrolmentId',
  Enquiry: 'enquiryId',
  Application: 'applicationId',
  FinancialRecord: 'financialRecordId',
  MigrationCase: 'migrationCaseId'
};

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
