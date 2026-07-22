import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

type PickerMode = 'datetime' | 'date' | 'time';

@Component({
  selector: 'app-datetime-picker',
  standalone: true,
  imports: [CommonModule],
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

  value = '';
  disabled = false;

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  get inputType(): string {
    switch (this.mode) {
      case 'date': return 'date';
      case 'time': return 'time';
      default: return 'datetime-local';
    }
  }

  writeValue(value: Date | string | null): void {
    this.value = this.toInputString(value);
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

  onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    this.value = raw;
    this.onChange(raw || null);
  }

  onBlur(): void {
    this.onTouched();
  }

  private toInputString(value: Date | string | null | undefined): string {
    if (!value) return '';
    const date = value instanceof Date ? value : new Date(value);
    if (isNaN(date.getTime())) return '';

    const pad = (n: number) => String(n).padStart(2, '0');
    const y = date.getFullYear();
    const mo = pad(date.getMonth() + 1);
    const d = pad(date.getDate());
    const h = pad(date.getHours());
    const mi = pad(date.getMinutes());

    switch (this.mode) {
      case 'date': return `${y}-${mo}-${d}`;
      case 'time': return `${h}:${mi}`;
      default: return `${y}-${mo}-${d}T${h}:${mi}`;
    }
  }
}