import { Injectable, Inject, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { TranslateService, InterpolatableTranslationObject } from '@ngx-translate/core';
import { Observable } from 'rxjs';

const LANG_STORAGE_KEY = 'eduflex_lang';
const SUPPORTED_LANGS = ['en', 'vi'];
const DEFAULT_LANG = 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);
  private isBrowser: boolean;

  constructor(@Inject(PLATFORM_ID) platformId: Object) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

 init(): Observable<InterpolatableTranslationObject> {
    const stored = this.isBrowser ? localStorage.getItem(LANG_STORAGE_KEY) : null;
    const lang = stored && SUPPORTED_LANGS.includes(stored) ? stored : DEFAULT_LANG;
    return this.translate.use(lang);
  }

  get currentLang(): string {
    return this.translate.currentLang() || DEFAULT_LANG;
  }

  setLang(lang: string): void {
    this.translate.use(lang);
    if (this.isBrowser) {
      localStorage.setItem(LANG_STORAGE_KEY, lang);
    }
  }
}
