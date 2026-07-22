import { Component, AfterViewInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-student-handbook',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './student-handbook.component.html',
  styleUrls: ['./student-handbook.component.css']
})
export class StudentHandbookComponent implements AfterViewInit, OnDestroy {
  private observer?: IntersectionObserver;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId) || !('IntersectionObserver' in window)) return;

    const links = Array.from(document.querySelectorAll<HTMLAnchorElement>('.handbook-toc a'));
    const sections = links
      .map(link => document.getElementById(link.getAttribute('href')!.slice(1)))
      .filter((el): el is HTMLElement => !!el);

    this.observer = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        const index = sections.indexOf(entry.target as HTMLElement);
        if (index === -1 || !entry.isIntersecting) return;
        links.forEach(l => l.classList.remove('active'));
        links[index].classList.add('active');
      });
    }, { rootMargin: '-15% 0px -70% 0px', threshold: 0 });

    sections.forEach(section => this.observer!.observe(section));
  }

  jumpTo(event: Event, id: string): void {
    event.preventDefault();
    if (!isPlatformBrowser(this.platformId)) return;
    const el = document.getElementById(id);
    if (!el) return;
    history.pushState(null, '', '#' + id);
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    el.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}