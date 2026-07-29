import { Injectable } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { Client, SettingsDto } from './api.services';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private settings$: Observable<SettingsDto> | null = null;

  constructor(private apiClient: Client) {}

  getSettings(): Observable<SettingsDto> {
    if (!this.settings$) {
      this.settings$ = this.apiClient.settingsGET().pipe(shareReplay(1));
    }
    return this.settings$;
  }
}
