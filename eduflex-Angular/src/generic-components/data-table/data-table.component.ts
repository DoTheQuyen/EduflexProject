import { Component, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import type { TableLazyLoadEvent } from 'primeng/table';

import { DataTableColumn, DataTableAction, DataTableRowAction } from './data-table.models';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, TableModule],
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.css'],
})
export class DataTableComponent<T> {
  @Input() columns: DataTableColumn<T>[] = [];
  @Input() data: T[] = [];
  @Input() tableClass: string = 'table table-bordered table-hover mb-0';
  @Input() rowActions: DataTableRowAction<T>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' },
  ];

  @Input() pageNumber = 1;
  @Input() pageSize = 10;
  @Input() totalCount = 0;
  @Output() pageChange = new EventEmitter<number>();

  @Input() showSearch = false;
  @Input() searchPlaceholder = 'Search...';
  @Output() search = new EventEmitter<string>();
  @Output() refresh = new EventEmitter<void>();

  @Output() actionClick = new EventEmitter<DataTableAction<T>>();
  // order: 1 = ascending, -1 = descending (PrimeNG's own SortMeta convention).
  @Output() sortChange = new EventEmitter<{ field: string; order: number }>();

  private sortField: string | null = null;
  private sortOrder = 1;

  @ViewChild('searchInputEl') searchInputEl?: ElementRef<HTMLInputElement>;

  onSearchClick(): void {
    this.search.emit(this.searchInputEl?.nativeElement.value ?? '');
  }

  onRefreshClick(): void {
    if (this.searchInputEl) {
      this.searchInputEl.nativeElement.value = '';
    }
    this.refresh.emit();
  }

  get first(): number {
    return (this.pageNumber - 1) * this.pageSize;
  }

  // The table only ever has the current page's rows loaded (server-side paging), so
  // this sorts what's currently on screen — not the full underlying dataset. That's
  // the deliberate tradeoff for working, zero-backend-change sorting; a consumer that
  // wants true full-dataset sorting can still listen to (sortChange) and refetch.
  get sortedData(): T[] {
    if (!this.sortField) return this.data;
    const field = this.sortField;
    const order = this.sortOrder;
    return [...this.data].sort((a, b) => {
      const va = this.getCellData(a, field);
      const vb = this.getCellData(b, field);
      if (va === vb) return 0;
      return va > vb ? order : -order;
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;
    const page = rows === 0 ? 1 : Math.floor(first / rows) + 1;
    if (page !== this.pageNumber) {
      this.pageChange.emit(page);
    }

    if (typeof event.sortField === 'string' && event.sortOrder != null) {
      this.sortField = event.sortField;
      this.sortOrder = event.sortOrder;
      this.sortChange.emit({ field: event.sortField, order: event.sortOrder });
    }
  }

  // Pairs with the @media rules in data-table.component.css. Widest net wins, since
  // each tier's max-width already covers every smaller one.
  hideClass(col: DataTableColumn<T>): string {
    if (col.hideOnLaptop) return 'col-hide-laptop';
    if (col.hideOnTablet) return 'col-hide-tablet';
    if (col.hideOnMobile) return 'col-hide-mobile';
    return '';
  }

  isSortable(col: DataTableColumn<T>): boolean {
    return col.sortable !== false && col.field !== 'actions';
  }

  fieldName(col: DataTableColumn<T>): string {
    return String(col.field);
  }

  // Nested field accessor
  getCellData<T>(
    row: T,
    field: keyof T | string,
    formatter?: (value: T[keyof T], row: T) => string | number,
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
      case 'Approved':
        return 'bg-success';
      case 'Rejected':
        return 'bg-danger';
      case 'Studying':
        return 'bg-primary';
      case 'Under Review':
        return 'bg-warning text-dark';
      case 'Documents Required':
        return 'bg-info text-dark';
      default:
        return 'bg-secondary';
    }
  }
}
