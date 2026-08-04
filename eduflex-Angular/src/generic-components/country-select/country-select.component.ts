import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { COUNTRIES } from '@app/models/countries';

@Component({
  selector: 'app-country-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './country-select.component.html',
  styleUrls: ['./country-select.component.css'],
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

  query = '';
  open = false;
  highlightedIndex = -1;
  disabled = false;
  committedValue = '';

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  get filteredCountries(): string[] {
    const term = this.query.trim().toLowerCase();
    if (!term) return COUNTRIES.slice(0, 8);
    return COUNTRIES.filter(c => c.toLowerCase().includes(term)).slice(0, 8);
  }

  writeValue(value: string | null): void {
    this.committedValue = value ?? '';
    this.query = this.committedValue;
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
    this.query = (event.target as HTMLInputElement).value;
    this.open = true;
    this.highlightedIndex = -1;
  }

  onFocus(): void {
    this.open = true;
  }

  selectCountry(country: string): void {
    this.committedValue = country;
    this.query = country;
    this.onChange(country);
    this.open = false;
    this.highlightedIndex = -1;
  }

  onOptionMouseDown(event: MouseEvent, country: string): void {
    // Stops the input's blur handler firing before the click registers,
    // which would otherwise close the dropdown out from under it.
    event.preventDefault();
    this.selectCountry(country);
  }

  onKeydown(event: KeyboardEvent): void {
    const options = this.filteredCountries;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.open = true;
      this.highlightedIndex = Math.min(this.highlightedIndex + 1, options.length - 1);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.highlightedIndex = Math.max(this.highlightedIndex - 1, 0);
    } else if (event.key === 'Enter') {
      if (this.open && this.highlightedIndex >= 0 && options[this.highlightedIndex]) {
        event.preventDefault();
        this.selectCountry(options[this.highlightedIndex]);
      }
    } else if (event.key === 'Escape') {
      this.open = false;
    }
  }

  onBlur(): void {
    this.open = false;
    const trimmed = this.query.trim();
    if (!trimmed) {
      this.committedValue = '';
      this.query = '';
      this.onChange(null);
    } else if (trimmed !== this.committedValue) {
      // Typed text that was never picked from the list — discard it.
      this.query = this.committedValue;
    }
    this.onTouched();
  }
}