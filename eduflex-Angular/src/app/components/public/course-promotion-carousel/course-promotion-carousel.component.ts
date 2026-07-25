import { Component, EventEmitter, Inject, OnDestroy, OnInit, Output, PLATFORM_ID } from '@angular/core';
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
  @Output() enquire = new EventEmitter<void>();

  coursePromotions: CoursePromotionDto[] = [];
  currentPromotionIndex: number = 0;
  private promotionAutoplayTimer: any;

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
    this.apiClient.courseLatest(10).subscribe({
      next: (promotions) => {
        this.coursePromotions = promotions;
        this.restartPromotionAutoplay();
      },
      error: (err) => {
        console.error('Failed to load course promotions', err);
      }
    });
  }

  goToPromotionSlide(index: number): void {
    this.currentPromotionIndex = index;
  }

  nextPromotionSlide(): void {
    if (this.coursePromotions.length === 0) { return; }
    this.currentPromotionIndex = (this.currentPromotionIndex + 1) % this.coursePromotions.length;
  }

  prevPromotionSlide(): void {
    if (this.coursePromotions.length === 0) { return; }
    this.currentPromotionIndex = (this.currentPromotionIndex - 1 + this.coursePromotions.length) % this.coursePromotions.length;
  }

  startPromotionAutoplay(): void {
    this.stopPromotionAutoplay();
    this.promotionAutoplayTimer = setInterval(() => this.nextPromotionSlide(), 5000);
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
    this.enquire.emit();
  }
}