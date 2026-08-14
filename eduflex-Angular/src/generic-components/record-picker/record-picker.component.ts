import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { ModalComponent } from '../modal/modal.component';
import { DataTableComponent } from '../data-table/data-table.component';
import { DataTableAction, DataTableColumn, DataTableRowAction } from '../data-table/data-table.models';

export interface RecordPickerPage<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/**
 * Generic search-and-pick popup: composes the existing app-modal + app-data-table
 * building blocks (no new UI kit), same convention as every other feature's ad-hoc
 * "modal + data-table" picker (e.g. enrolment-new's student search step) — just
 * extracted into a reusable component instead of hand-rolled per feature. The caller
 * supplies the search function and column config for whatever record type it's
 * picking; this component has no idea whether it's picking an Enrolment, Enquiry,
 * Application or FinancialRecord — that's entirely the caller's concern, including
 * checking the caller has permission to see that record type before opening this.
 *
 * Mount/unmount with *ngIf at the call site (same as every other app-modal usage in
 * this codebase) — there's no internal `visible` input, matching ModalComponent's own
 * `[visible]="true"` hardcoded-when-mounted convention.
 */
@Component({
  selector: 'app-record-picker',
  standalone: true,
  imports: [CommonModule, ModalComponent, DataTableComponent],
  templateUrl: './record-picker.component.html'
})
export class RecordPickerComponent<T = any> implements OnInit {
  @Input() title = 'Select a record';
  @Input() searchPlaceholder = 'Search...';
  @Input() columns: DataTableColumn<T>[] = [];
  @Input() pageSize = 10;
  @Input({ required: true }) searchFn!: (searchTerm: string, pageNumber: number, pageSize: number) => Observable<RecordPickerPage<T>>;

  @Output() closeModal = new EventEmitter<void>();
  @Output() picked = new EventEmitter<T>();

  results: T[] = [];
  totalCount = 0;
  pageNumber = 1;
  searchTerm = '';

  readonly rowActions: DataTableRowAction<T>[] = [
    { action: 'select', label: 'Select', icon: 'fa-check', cssClass: 'btn btn-sm btn-primary' }
  ];

  ngOnInit(): void {
    this.runSearch();
  }

  onSearch(term: string): void {
    this.searchTerm = term;
    this.pageNumber = 1;
    this.runSearch();
  }

  onRefresh(): void {
    this.runSearch();
  }

  onPageChange(page: number): void {
    this.pageNumber = page;
    this.runSearch();
  }

  onTableAction(event: DataTableAction<T>): void {
    if (event.action === 'select') {
      this.picked.emit(event.row);
    }
  }

  onClose(): void {
    this.closeModal.emit();
  }

  private runSearch(): void {
    this.searchFn(this.searchTerm, this.pageNumber, this.pageSize).subscribe({
      next: (result) => {
        this.results = result.items;
        this.totalCount = result.totalCount;
      }
    });
  }
}
