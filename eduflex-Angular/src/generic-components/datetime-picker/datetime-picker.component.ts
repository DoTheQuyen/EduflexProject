import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';

type PickerMode = 'datetime' | 'date' | 'time';

@Component({
  selector: 'app-datetime-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePicker],
  templateUrl: './datetime-picker.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateTimePickerComponent),
      multi: true
    }
  ]
})
export class DateTimePickerComponent implements ControlValueAccessor {
  @Input() mode: PickerMode = 'datetime';
  @Input() label = '';
  @Input() required = false;
  @Input() min?: string;
  @Input() max?: string;

  dateValue: Date | null = null;
  disabled = false;

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  get showTime(): boolean {
    return this.mode === 'datetime';
  }

  get timeOnly(): boolean {
    return this.mode === 'time';
  }

  get dateFormat(): string {
    return 'yy-mm-dd';
  }

  get minDate(): Date | undefined {
    return this.min ? this.toDate(this.min) ?? undefined : undefined;
  }

  get maxDate(): Date | undefined {
    return this.max ? this.toDate(this.max) ?? undefined : undefined;
  }

  writeValue(value: Date | string | null): void {
    this.dateValue = this.toDate(value);
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onModelChange(value: Date | null): void {
    this.dateValue = value;
    this.onChange(this.toOutputString(value));
  }

  onBlur(): void {
    this.onTouched();
  }

  private toDate(value: Date | string | null | undefined): Date | null {
    if (!value) return null;
    const date = value instanceof Date ? value : new Date(value);
    return isNaN(date.getTime()) ? null : date;
  }

  private toOutputString(value: Date | null): string | null {
    if (!value || isNaN(value.getTime())) return null;

    const pad = (n: number) => String(n).padStart(2, '0');
    const y = value.getFullYear();
    const mo = pad(value.getMonth() + 1);
    const d = pad(value.getDate());
    const h = pad(value.getHours());
    const mi = pad(value.getMinutes());

    switch (this.mode) {
      case 'date': return `${y}-${mo}-${d}`;
      case 'time': return `${h}:${mi}`;
      default: return `${y}-${mo}-${d}T${h}:${mi}`;
    }
  }
}
