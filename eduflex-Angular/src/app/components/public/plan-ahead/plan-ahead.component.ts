import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

/** Static content page. Structured as a timeline rather than a topic list
 *  because every item here is anchored to a moment — the advice is worthless
 *  if it's read after the moment has passed. No booking CTA: the page is
 *  meant to read as genuinely agent-agnostic advice, not a funnel. */
@Component({
  selector: 'app-plan-ahead',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './plan-ahead.component.html',
  styleUrls: ['./plan-ahead.component.css']
})
export class PlanAheadComponent {
  /** Held as a field, not an array literal in the template — a literal is a new
   *  identity every change-detection pass, so ngFor would rebuild the rows. */
  readonly graduationDocs = ['DOC1', 'DOC2', 'DOC3', 'DOC4', 'DOC5'];
}
