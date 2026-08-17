import { Component, Inject, OnInit, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Client, FeedbackDto } from '@services/content.services';
import { TranslatePipe } from '@ngx-translate/core';
import { stripHtml } from '@app/shared/utils/strip-html.util';

/** Comments longer than this get clamped with a "read more" toggle, so one
 *  essay-length review can't set the height of an entire grid row. Chosen by
 *  character count rather than measuring the rendered box: it needs no layout
 *  read, so the toggle never flickers in or out as fonts and images settle. */
const CLAMP_THRESHOLD = 320;

@Component({
  selector: 'app-feedback',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './feedback.component.html',
  styleUrls: ['./feedback.component.css']
})
export class FeedbackComponent implements OnInit {
  feedbacks: FeedbackDto[] = [];
  isLoading = true;
  hasError = false;

  /** Held as a field, not an array literal in the template — an inline literal
   *  is a new identity every change-detection pass, so ngFor would tear down
   *  and rebuild the placeholder cards on each one. */
  readonly skeletonSlots = [1, 2, 3, 4, 5, 6];

  private readonly expandedIds = new Set<string>();

  constructor(
    private apiClient: Client,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadFeedbacks();
    }
  }

  loadFeedbacks(): void {
    this.isLoading = true;
    this.hasError = false;

    this.apiClient.feedbackLatest(100).subscribe({
      next: (feedbacks) => {
        this.feedbacks = feedbacks;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load feedbacks', err);
        this.hasError = true;
        this.isLoading = false;
      }
    });
  }

  /** Distinct courses represented, shown as a headline figure. */
  get courseCount(): number {
    const courses = this.feedbacks
      .map((f) => f.courseName?.trim())
      .filter((name): name is string => !!name);
    return new Set(courses).size;
  }

  isLong(comment: string | undefined): boolean {
    return stripHtml(comment ?? '').length > CLAMP_THRESHOLD;
  }

  isExpanded(feedback: FeedbackDto): boolean {
    return this.expandedIds.has(this.keyFor(feedback));
  }

  toggleExpanded(feedback: FeedbackDto): void {
    const key = this.keyFor(feedback);
    if (this.expandedIds.has(key)) {
      this.expandedIds.delete(key);
    } else {
      this.expandedIds.add(key);
    }
  }

  /** Fallback avatar for feedback with no photo: up to two initials. */
  initials(name: string | undefined): string {
    if (!name) { return ''; }
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');
  }

  private keyFor(feedback: FeedbackDto): string {
    return feedback.id ?? `${feedback.name}|${feedback.comment}`;
  }
}
