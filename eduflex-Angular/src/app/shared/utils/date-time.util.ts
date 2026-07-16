import { DatePipe } from '@angular/common';

const datePipe = new DatePipe('en-AU'); // change locale if needed

/**
 * Formats a date or date string into a given format
 * @param value Date | string | null
 * @param format Angular DatePipe format string (default: 'dd/MM/yyyy HH:mm')
 */
export function formatDateTime(value: Date | string | null, format: string = 'dd/MM/yyyy HH:mm'): string {
  if (!value) return '';
  try {
    return datePipe.transform(value, format) ?? '';
  } catch {
    return String(value);
  }
}
