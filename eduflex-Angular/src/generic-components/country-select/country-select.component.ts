import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { AutoComplete } from 'primeng/autocomplete';
import { COUNTRIES } from '@app/models/countries';

@Component({
  selector: 'app-country-select',
  standalone: true,
  imports: [CommonModule, FormsModule, AutoComplete],
  templateUrl: './country-select.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CountrySelectComponent),
      multi: true
    }
  ]
})
export class CountrySelectComponent implements ControlValueAccessor {
  @Input() id = '';
  @Input() placeholder = 'Start typing a country…';

  value: string | null = null;
  suggestions: string[] = COUNTRIES.slice(0, 8);
  disabled = false;

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value = value ?? null;
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

  search(event: { query: string }): void {
    const term = event.query.trim().toLowerCase();
    this.suggestions = term
      ? COUNTRIES.filter(c => c.toLowerCase().includes(term)).slice(0, 8)
      : COUNTRIES.slice(0, 8);
  }

  onModelChange(value: string | null): void {
    this.value = value;
    this.onChange(value || null);
  }

  onBlur(): void {
    this.onTouched();
  }
}
