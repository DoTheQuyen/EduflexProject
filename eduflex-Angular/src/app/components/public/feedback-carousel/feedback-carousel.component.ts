import { Component, Inject, OnDestroy, OnInit, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Client, FeedbackDto } from '@services/content.services';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-feedback-carousel',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './feedback-carousel.component.html',
  styleUrls: ['./feedback-carousel.component.css']
})
export class FeedbackCarouselComponent implements OnInit, OnDestroy {
  feedbacks: FeedbackDto[] = [];
  currentFeedbackIndex: number = 0;
  private feedbackAutoplayTimer: any;

  constructor(
    private apiClient: Client,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadFeedbacks();
    }
  }

  ngOnDestroy(): void {
    this.stopFeedbackAutoplay();
  }

  loadFeedbacks(): void {

     this.apiClient.feedbackLatest(10).subscribe({
      next: (feedbacks) => {
        this.feedbacks = feedbacks;
        this.restartFeedbackAutoplay();
      },
      error: (err) => {
        console.error('Failed to load course promotions', err);
      }
    });

  }

  goToFeedbackSlide(index: number): void {
    this.currentFeedbackIndex = index;
  }

  nextFeedbackSlide(): void {
    if (this.feedbacks.length === 0) { return; }
    this.currentFeedbackIndex = (this.currentFeedbackIndex + 1) % this.feedbacks.length;
  }

  prevFeedbackSlide(): void {
    if (this.feedbacks.length === 0) { return; }
    this.currentFeedbackIndex = (this.currentFeedbackIndex - 1 + this.feedbacks.length) % this.feedbacks.length;
  }

  startFeedbackAutoplay(): void {
    this.stopFeedbackAutoplay();
    this.feedbackAutoplayTimer = setInterval(() => this.nextFeedbackSlide(), 6000);
  }

  stopFeedbackAutoplay(): void {
    if (this.feedbackAutoplayTimer) {
      clearInterval(this.feedbackAutoplayTimer);
    }
  }

  restartFeedbackAutoplay(): void {
    if (this.feedbacks.length > 1) {
      this.startFeedbackAutoplay();
    }
  }
}