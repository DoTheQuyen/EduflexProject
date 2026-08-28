import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UIChart } from 'primeng/chart';
import { StatusCountDto } from '../../../../../services/api.services';
import { barGradient, ensureChartJsRegistered, lighten, reducedMotionPreferred } from '../chart-setup';

ensureChartJsRegistered();

@Component({
  selector: 'app-status-breakdown-chart',
  standalone: true,
  imports: [CommonModule, UIChart],
  templateUrl: './status-breakdown-chart.component.html',
  styleUrls: ['./status-breakdown-chart.component.css'],
})
export class StatusBreakdownChartComponent implements OnChanges {
  @Input() title = '';
  @Input() color = '#16233a';
  @Input() items: StatusCountDto[] | null = null;

  chartData: any;
  readonly chartOptions: any;

  constructor() {
    this.chartOptions = {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      animation: reducedMotionPreferred() ? false : { duration: 300 },
      plugins: {
        legend: { display: false },
        tooltip: { mode: 'nearest', intersect: true },
      },
      scales: {
        x: { beginAtZero: true, grid: { display: false }, ticks: { precision: 0 } },
        y: { grid: { display: false } },
      },
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items']) {
      this.buildChartData();
    }
  }

  get total(): number {
    return (this.items ?? []).reduce((sum, i) => sum + (i.count ?? 0), 0);
  }

  get chartSummary(): string {
    const parts = (this.items ?? []).map((i) => `${i.label}: ${i.count ?? 0}`).join(', ');
    return `Bar chart showing current ${this.title} status breakdown — ${parts || 'no data'}.`;
  }

  private buildChartData(): void {
    const items = this.items ?? [];
    this.chartData = {
      labels: items.map((i) => i.label),
      datasets: [
        {
          data: items.map((i) => i.count ?? 0),
          backgroundColor: barGradient(this.color),
          borderColor: lighten(this.color, 0.35),
          borderWidth: 1,
          borderRadius: 4,
          maxBarThickness: 22,
        },
      ],
    };
  }
}
