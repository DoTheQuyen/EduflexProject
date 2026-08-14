export interface DataTableColumn<T = any> {
  field: keyof T | string;
  title: string;
  className?: string;
  formatter?: (value: any, row?: any) => any;
  render?: (value: unknown, row: T) => string;
  // Sortable by default (set false to opt out — e.g. a column that isn't real
  // sortable data). Sorting happens client-side over whatever page is currently
  // loaded; a consumer that needs full-dataset sorting can listen to (sortChange).
  sortable?: boolean;
  // Explicit minimum width (e.g. '150px'). Every PrimeNG scrollable table needs
  // this on every column or horizontal scroll never actually triggers — defaults
  // to 120px if not set.
  minWidth?: string;
  // Hide this column on smaller screens so the table fits instead of scrolling
  // sideways. All default to false — a column that sets none is always shown.
  //   hideOnLaptop: hidden below 1600px (laptops, tablets and phones)
  //   hideOnTablet: hidden below 992px  (tablets and phones)
  //   hideOnMobile: hidden below 768px  (phones only)
  // Each tier cascades down: hiding on laptop necessarily hides on tablet and
  // mobile too, so you only ever need to set the widest one. Use hideOnLaptop for
  // the columns you'd give up first.
  hideOnLaptop?: boolean;
  hideOnTablet?: boolean;
  hideOnMobile?: boolean;
}

export interface DataTableAction<T> {
  action: string;
  row: T;
}

export interface DataTableRowAction<T = any> {
  action: string;
  label: string;
  icon?: string;
  cssClass?: string;
  isVisible?: (row: T) => boolean;
}
