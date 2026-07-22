import $ from 'jquery';
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';   // ✅ import your config

(window as any).$ = $;
(window as any).jQuery = $;

bootstrapApplication(AppComponent, appConfig)
  .catch(err => console.error(err));