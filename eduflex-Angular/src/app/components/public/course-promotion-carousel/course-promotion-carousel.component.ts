import { Component, ElementRef, EventEmitter, Inject, OnDestroy, OnInit, Output, PLATFORM_ID, ViewChild } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Client, CoursePromotionDto } from '@services/content.services';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-course-promotion-carousel',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './course-promotion-carousel.component.html',
  styleUrls: ['./course-promotion-carousel.component.css']
})
export class CoursePromotionCarouselComponent implements OnInit, OnDestroy {
  @Output() enquire = new EventEmitter<CoursePromotionDto>();

  coursePromotions: CoursePromotionDto[] = [];
  currentPromotionIndex: number = 0;
  /** True until the first response (success or failure) arrives. Starts true
   *  on the server too, so the skeleton below — not a blank gap — is what
   *  actually ships in the prerendered HTML; isPlatformBrowser only gates the
   *  HTTP call itself, not this initial state. */
  isLoading = true;
  hasError = false;
  private promotionAutoplayTimer: any;

  @ViewChild('noteEl') noteEl?: ElementRef<HTMLElement>;
  /** Only true once the note's content actually overflows its max-height — the
   *  fade-out gradient (::after on .promo-note) is gated on this class so it
   *  doesn't sit on top of (and dim) a short note that never gets clipped. */
  noteIsTruncated = false;

  constructor(
    private apiClient: Client,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadCoursePromotions();
    }
  }

  ngOnDestroy(): void {
    this.stopPromotionAutoplay();
  }

  loadCoursePromotions(): void {
    this.isLoading = true;
    this.hasError = false;

    this.apiClient.courseLatest(10).subscribe({
      next: (promotions) => {
        this.coursePromotions = promotions;
        this.isLoading = false;
        this.restartPromotionAutoplay();
        this.checkNoteTruncation();
      },
      error: (err) => {
        console.error('Failed to load course promotions', err);
        this.hasError = true;
        this.isLoading = false;
      }
    });
  }

  goToPromotionSlide(index: number): void {
    this.currentPromotionIndex = index;
    this.checkNoteTruncation();
  }

  nextPromotionSlide(): void {
    if (this.coursePromotions.length === 0) { return; }
    this.currentPromotionIndex = (this.currentPromotionIndex + 1) % this.coursePromotions.length;
    this.checkNoteTruncation();
  }

  prevPromotionSlide(): void {
    if (this.coursePromotions.length === 0) { return; }
    this.currentPromotionIndex = (this.currentPromotionIndex - 1 + this.coursePromotions.length) % this.coursePromotions.length;
    this.checkNoteTruncation();
  }

  /** Runs after the DOM updates for the new slide, so scrollHeight/clientHeight
   *  reflect the note that's now actually rendered. */
  private checkNoteTruncation(): void {
    setTimeout(() => {
      const el = this.noteEl?.nativeElement;
      this.noteIsTruncated = !!el && el.scrollHeight > el.clientHeight + 1;
    });
  }

  startPromotionAutoplay(): void {
    this.stopPromotionAutoplay();
    this.promotionAutoplayTimer = setInterval(() => this.nextPromotionSlide(), 10000);
  }

  stopPromotionAutoplay(): void {
    if (this.promotionAutoplayTimer) {
      clearInterval(this.promotionAutoplayTimer);
    }
  }

  restartPromotionAutoplay(): void {
    if (this.coursePromotions.length > 1) {
      this.startPromotionAutoplay();
    }
  }

onEnquireClick(): void {
  this.enquire.emit(this.coursePromotions[this.currentPromotionIndex]);
}
}