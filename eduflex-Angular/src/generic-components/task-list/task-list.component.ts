import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataTableComponent } from '../data-table/data-table.component';
import { DataTableAction, DataTableColumn, DataTableRowAction } from '../data-table/data-table.models';
import { TaskService } from '../../app/services/task.service';
import { UserDirectoryService } from '../../app/services/user-directory.service';
import { LINKED_RECORD_FILTER_FIELD, LinkedRecordType, Task, TaskFilter, TASK_STATUS_LABELS, taskStatusBadgeClass } from '../../app/models/task';

export type TaskListMode = 'my' | 'all' | 'linked';

/**
 * Reusable Task list — backs My Tasks, the Manager-only All Tasks page, and the Tasks
 * tab on Enrolment/FinancialRecord (and, later, Enquiry/Application) detail pages. One
 * component, three data sources (TaskService.searchMyTasks/searchAllTasks/
 * searchLinkedTasks), same as the module spec's "build as generic as possible to
 * reuse" requirement. New/Processing tasks sit together in the "Active" tab (badge
 * column shows which); Completed tasks get their own tab with its own search, per spec.
 */
@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './task-list.component.html'
})
export class TaskListComponent implements OnInit, OnChanges {
  @Input({ required: true }) mode!: TaskListMode;

  // 'linked' mode only.
  @Input() linkedRecordType?: LinkedRecordType;
  @Input() linkedRecordId?: string;

  @Input() pageSize = 10;
  @Input() showAddButton = true;

  @Output() viewTask = new EventEmitter<string>();
  @Output() addTask = new EventEmitter<void>();

  activeTab: 'active' | 'completed' = 'active';

  tasks: Task[] = [];
  totalCount = 0;
  pageNumber = 1;
  searchTerm = '';

  readonly rowActions: DataTableRowAction<Task>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' }
  ];

  columns: DataTableColumn<Task>[] = [];

  private initialized = false;

  constructor(
    private taskService: TaskService,
    private userDirectory: UserDirectoryService
  ) {}

  ngOnInit(): void {
    this.buildColumns();
    this.userDirectory.load().subscribe(() => {
      // Names resolve from raw ids to display names once the directory loads —
      // rebuild the already-fetched rows' rendering by re-running column formatters.
      this.tasks = [...this.tasks];
    });
    this.initialized = true;
    this.load();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // ngOnChanges fires before ngOnInit on first binding too — skip that first call so
    // the initial load (triggered from ngOnInit, after buildColumns() has run) doesn't
    // race a second one fired from here with columns not yet built.
    if (!this.initialized) return;

    if (changes['mode'] || changes['linkedRecordId'] || changes['linkedRecordType']) {
      this.pageNumber = 1;
      this.load();
    }
  }

  switchTab(tab: 'active' | 'completed'): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.pageNumber = 1;
    this.searchTerm = '';
    this.load();
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
    this.load();
  }

  onRefresh(): void {
    this.load();
  }

  onPageChange(page: number): void {
    this.pageNumber = page;
    this.load();
  }

  onTableAction(event: DataTableAction<Task>): void {
    if (event.action === 'view') {
      this.viewTask.emit(event.row.id);
    }
  }

  onAddClick(): void {
    this.addTask.emit();
  }

  private buildColumns(): void {
    this.columns = [
      { field: 'name', title: 'Task', minWidth: '200px' },
      {
        field: 'assigneeUserId', title: 'Assignee', minWidth: '150px',
        formatter: (value) => this.userDirectory.getName(value as string)
      },
      {
        field: 'assignerUserId', title: 'Assigner', minWidth: '150px', hideOnTablet: true,
        formatter: (value) => this.userDirectory.getName(value as string)
      },
      {
        field: 'dueDateTime', title: 'Due', minWidth: '160px',
        formatter: (value) => value ? new Date(value as string).toLocaleString() : ''
      },
      {
        field: 'status', title: 'Status', minWidth: '120px', className: 'text-center',
        render: (value) => `<span class="badge-pill ${taskStatusBadgeClass(value as any)}">${TASK_STATUS_LABELS[value as keyof typeof TASK_STATUS_LABELS] ?? value}</span>`
      },
      { field: 'actions', title: '', minWidth: '90px' }
    ];
  }

  private buildFilter(): TaskFilter {
    const filter: TaskFilter = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || undefined,
      status: this.activeTab === 'completed' ? 'Completed' : undefined,
      excludeStatus: this.activeTab === 'active' ? 'Completed' : undefined
    };

    if (this.mode === 'linked' && this.linkedRecordType && this.linkedRecordId) {
      (filter as any)[LINKED_RECORD_FILTER_FIELD[this.linkedRecordType]] = this.linkedRecordId;
    }

    return filter;
  }

  private load(): void {
    if (this.mode === 'linked' && !this.linkedRecordId) {
      this.tasks = [];
      this.totalCount = 0;
      return;
    }

    const filter = this.buildFilter();
    const search$ =
      this.mode === 'my' ? this.taskService.searchMyTasks(filter) :
      this.mode === 'all' ? this.taskService.searchAllTasks(filter) :
      this.taskService.searchLinkedTasks(filter);

    search$.subscribe({
      next: (result) => {
        // "Active" tab shows New + Processing together — the backend filter only
        // narrows to Completed for the Completed tab, so no client-side filtering
        // needed here beyond what searchTerm/status already did server-side.
        this.tasks = result.items;
        this.totalCount = result.totalCount;
      },
      error: () => {
        this.tasks = [];
        this.totalCount = 0;
      }
    });
  }
}
