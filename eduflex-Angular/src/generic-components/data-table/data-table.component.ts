import { Component, Input, Output, EventEmitter, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { DataTableColumn, DataTableAction, DataTableRowAction } from './data-table.models';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.css']
})
export class DataTableComponent<T> implements OnDestroy {
  @Input() columns: DataTableColumn<T>[] = [];
  @Input() data: T[] = [];
  @Input() tableClass: string = 'table table-bordered table-hover mb-0';
  @Input() rowActions: DataTableRowAction<T>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' }
  ];

  @Input() pageNumber = 1;
  @Input() pageSize = 10;
  @Input() totalCount = 0;
  @Output() pageChange = new EventEmitter<number>();

  @Input() showSearch = false;
  @Input() searchPlaceholder = 'Search...';
  @Output() searchChange = new EventEmitter<string>();

  @Output() actionClick = new EventEmitter<DataTableAction<T>>();

  private searchInput$ = new Subject<string>();
  private searchSub: Subscription;

  constructor() {
    this.searchSub = this.searchInput$.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(term => this.searchChange.emit(term));
  }

  ngOnDestroy(): void {
    this.searchSub.unsubscribe();
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
  }

  get totalPages(): number {
    return this.pageSize === 0 ? 0 : Math.ceil(this.totalCount / this.pageSize);
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) {
      return;
    }
    this.pageChange.emit(page);
  }

  // Nested field accessor
  getCellData<T>(
    row: T,
    field: keyof T | string,
    formatter?: (value: T[keyof T], row: T) => string | number
  ): string | number {
    const value = (row as any)[field]; // safe access

    if (formatter) return formatter(value, row);

    if (value == null) return '';
    if (typeof value === 'string' || typeof value === 'number') return value;
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return JSON.stringify(value);
  }

  getStatusBadgeClass(status: unknown): string {
    const value = String(status);
    switch (value) {
      case 'Approved': return 'bg-success';
      case 'Rejected': return 'bg-danger';
      case 'Studying': return 'bg-primary';
      case 'Under Review': return 'bg-warning text-dark';
      case 'Documents Required': return 'bg-info text-dark';
      default: return 'bg-secondary';
    }
  }
}
