// export interface DataTableColumn<T> {
//   title: string;                           // Header text
//   field: keyof T | string;                 // Property name in T (supports nested dot notation)
//   className?: string;                      // Optional CSS class for alignment
//   render?: (value: unknown, row: T) => string; // Optional custom renderer
// }

export interface DataTableColumn<T = any> {
  field: keyof T | string; 
  title: string;
  className?: string;
  formatter?: (value: any, row?: any) => any; 
   render?: (value: unknown, row: T) => string;
}

export interface DataTableSettings {
  pageLength?: number;
  lengthMenu?: number[];
  searching?: boolean;
  ordering?: boolean;
  responsive?: boolean;
}

export interface DataTableAction<T> {
  action: string;
  row: T;
}