import { AfterViewInit, Component, ElementRef, Inject, OnInit, PLATFORM_ID, ViewChild } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Client, FeedbackDto } from '@services/content.services';
import { TranslatePipe } from '@ngx-translate/core';

/** Comments longer than this get clamped with a "read more" toggle. Chosen by
 *  character count rather than measuring the rendered box: it needs no layout
 *  read, so the toggle never flickers in or out as fonts and images settle. */
const CLAMP_THRESHOLD = 260;

@Component({
  selector: 'app-feedback-carousel',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './feedback-carousel.component.html',
  styleUrls: ['./feedback-carousel.component.css']
})
export class FeedbackCarouselComponent implements OnInit, AfterViewInit {
  @ViewChild('track') trackRef?: ElementRef<HTMLElement>;

  feedbacks: FeedbackDto[] = [];
  atStart = true;
  atEnd = false;
  /** True until the first response (success or failure) arrives. Starts true
   *  on the server too, so the skeleton below — not a blank gap — is what
   *  actually ships in the prerendered HTML; isPlatformBrowser only gates the
   *  HTTP call itself, not this initial state. */
  isLoading = true;
  hasError = false;

  /** Held as a field, not an array literal in the template — an inline
   *  literal is a new identity every change-detection pass, so ngFor would
   *  tear down and rebuild the placeholder cards on each one. */
  readonly skeletonSlots = [1, 2, 3];

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

  ngAfterViewInit(): void {
    this.updateArrowState();
  }

  loadFeedbacks(): void {
    this.isLoading = true;
    this.hasError = false;

    this.apiClient.feedbackLatest(12).subscribe({
      next: (feedbacks) => {
        this.feedbacks = feedbacks;
        this.isLoading = false;
        // Wait for the cards to lay out before measuring the track.
        setTimeout(() => this.updateArrowState());
      },
      error: (err) => {
        console.error('Failed to load feedbacks', err);
        this.hasError = true;
        this.isLoading = false;
      }
    });
  }

  /** Scrolls by one card width, so the step matches however many cards the
   *  current breakpoint happens to show. */
  scrollByCard(direction: -1 | 1): void {
    const track = this.trackRef?.nativeElement;
    if (!track) { return; }
    const card = track.querySelector<HTMLElement>('.feedback-card');
    const step = card ? card.offsetWidth + 24 : track.clientWidth;
    // scrollBy's smooth behaviour isn't covered by the CSS reduced-motion
    // query, so it has to be opted out of explicitly.
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    track.scrollBy({ left: step * direction, behavior: reduceMotion ? 'auto' : 'smooth' });
  }

  updateArrowState(): void {
    const track = this.trackRef?.nativeElement;
    if (!track) { return; }
    const maxScroll = track.scrollWidth - track.clientWidth;
    this.atStart = track.scrollLeft <= 1;
    // 1px of slack: fractional widths mean scrollLeft rarely lands exactly.
    this.atEnd = track.scrollLeft >= maxScroll - 1;
  }

  isLong(comment: string | undefined): boolean {
    return (comment?.length ?? 0) > CLAMP_THRESHOLD;
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
