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
    // index.html hardcodes lang="en" for the prerendered/first-paint case;
    // this keeps it truthful once a real locale is known. CSS relies on it
    // too — :lang(vi) in styles.css swaps the display font for Vietnamese —
    // so a stale attribute would silently break that as well as
    // screen-reader pronunciation.
    this.translate.onLangChange.subscribe(({ lang }) => this.setDocumentLang(lang));
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

  private setDocumentLang(lang: string): void {
    if (this.isBrowser) {
      document.documentElement.lang = lang;
    }
  }
}
