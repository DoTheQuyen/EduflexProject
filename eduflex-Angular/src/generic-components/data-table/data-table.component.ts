import { Component, Input, Output, OnInit, OnDestroy, 
  Inject, PLATFORM_ID, EventEmitter
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { DataTablesModule } from 'angular-datatables'; 
import { isPlatformBrowser } from '@angular/common';


import { DataTableColumn, DataTableSettings, DataTableAction, DataTableRowAction } from './data-table.models';

type ADTSettingsCompat = any;
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, DataTablesModule],
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.css']
})



export class DataTableComponent<T> implements OnInit, OnDestroy {
    constructor(@Inject(PLATFORM_ID) private platformId: Object) {}
  @Input() columns: DataTableColumn<T>[] = [];
  @Input() data: T[] = [];
  @Input() tableClass: string = 'table table-bordered table-hover mb-0';
  @Input() settings?: DataTableSettings;
  @Input() rowActions: DataTableRowAction<T>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' }
  ];
  @Output() actionClick = new EventEmitter<DataTableAction<T>>();

  
  dtOptions: DataTables.Settings = {};
  dtTrigger: Subject<void> = new Subject<void>();

get compatOptions(): ADTSettingsCompat {
    return this.dtOptions as unknown as ADTSettingsCompat;
  }

  get compatTrigger(): Subject<ADTSettingsCompat> {
    return this.dtTrigger as unknown as Subject<ADTSettingsCompat>;
  }


  ngOnInit(): void {
    this.dtOptions = {
      paging: true,
      pageLength: this.settings?.pageLength ?? 10,
      lengthMenu: this.settings?.lengthMenu ?? [5, 10, 25, 50],
      searching: this.settings?.searching ?? true,
      ordering: this.settings?.ordering ?? true,
      responsive: this.settings?.responsive ?? true,
    };

    if (isPlatformBrowser(this.platformId)) {
      // Only trigger DataTables once the plugin has actually attached
      // itself to jQuery — triggering earlier races the dynamic import.
      import('datatables.net-bs5').then(() => {
        this.dtTrigger.next();
      });
    }
  }

  ngOnDestroy(): void {
    this.dtTrigger.unsubscribe();
  }

  // Nested field accessor
  // getCellData(row: T, field: keyof T | string): unknown {
  //   return (field as string).split('.').reduce(
  //     (acc: unknown, key: string) => (acc as Record<string, unknown>)?.[key],
  //     row as Record<string, unknown>
  //   );
  // }
  
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
    case 'Under Review': return 'bg-warning text-dark';
    case 'Documents Required': return 'bg-info text-dark';
    default: return 'bg-secondary';
  }
}

}
