export interface DataTableColumn<T = any> {
  field: keyof T | string;
  title: string;
  className?: string;
  formatter?: (value: any, row?: any) => any;
  render?: (value: unknown, row: T) => string;
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
}
