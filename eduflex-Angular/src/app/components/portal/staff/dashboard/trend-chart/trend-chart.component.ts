import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UIChart } from 'primeng/chart';
import { SelectButton } from 'primeng/selectbutton';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Client, MonthlyTrendPointDto } from '../../../../../services/api.services';
import { barGradient, ensureChartJsRegistered, lighten, reducedMotionPreferred } from '../chart-setup';

ensureChartJsRegistered();

type ModuleKey = 'enquiry' | 'application' | 'enrolment' | 'migrationCase';
type ViewKey = 'all' | ModuleKey;

interface SeriesDef {
  key: ModuleKey;
  label: string;
  color: string;
}

const SERIES: SeriesDef[] = [
  { key: 'enquiry', label: 'Enquiries', color: '#16233a' },
  { key: 'application', label: 'Applications', color: '#b8862f' },
  { key: 'enrolment', label: 'Enrolments', color: '#1f7a5c' },
  { key: 'migrationCase', label: 'Migration Cases', color: '#d2530c' },
];

@Component({
  selector: 'app-dashboard-trend-chart',
  standalone: true,
  imports: [CommonModule, FormsModule, UIChart, SelectButton, ProgressSpinner],
  templateUrl: './trend-chart.component.html',
  styleUrls: ['./trend-chart.component.css'],
})
export class TrendChartComponent implements OnInit {
  readonly periodOptions: { key: 1 | 3 | 12; label: string }[] = [
    { key: 1, label: '1M' },
    { key: 3, label: '3M' },
    { key: 12, label: '12M' },
  ];

  readonly viewOptions: { key: ViewKey; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'enquiry', label: 'Enquiries' },
    { key: 'application', label: 'Applications' },
    { key: 'enrolment', label: 'Enrolments' },
    { key: 'migrationCase', label: 'Migration Cases' },
  ];

  selectedMonths: 1 | 3 | 12 = 3;
  selectedView: ViewKey = 'all';
  loading = true;
  error = false;
  points: MonthlyTrendPointDto[] = [];

  // Reassigned (never mutated) on every load/view change — p-chart only redraws when it
  // sees a new object reference come through its `data` input.
  chartData: any;
  readonly chartOptions: any;

  constructor(private client: Client) {
    this.chartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      animation: reducedMotionPreferred() ? false : { duration: 300 },
      plugins: {
        legend: { display: true, position: 'top', labels: { boxWidth: 10, font: { size: 11 } } },
        tooltip: { mode: 'index', intersect: false },
      },
      scales: {
        x: { grid: { display: false } },
        y: { beginAtZero: true, ticks: { precision: 0 } },
      },
    };
  }

  ngOnInit(): void {
    this.load();
  }

  setMonths(months: 1 | 3 | 12): void {
    if (this.selectedMonths === months) return;
    this.selectedMonths = months;
    this.load();
  }

  setView(view: ViewKey): void {
    this.selectedView = view;
    this.buildChartData();
  }

  load(): void {
    this.loading = true;
    this.error = false;
    this.client.monthlyTrends(this.selectedMonths).subscribe({
      next: (result) => {
        this.points = result.points ?? [];
        this.loading = false;
        this.buildChartData();
      },
      error: () => {
        this.loading = false;
        this.error = true;
      },
    });
  }

  activeSeries(): SeriesDef[] {
    return this.selectedView === 'all' ? SERIES : SERIES.filter((s) => s.key === this.selectedView);
  }

  valueFor(point: MonthlyTrendPointDto, key: ModuleKey): number {
    return point[key] ?? 0;
  }

  monthLabel(key: string | undefined): string {
    if (!key) return '';
    const [year, month] = key.split('-').map(Number);
    return new Date(year, month - 1, 1).toLocaleDateString(undefined, { month: 'short', year: '2-digit' });
  }

  get chartSummary(): string {
    const seriesNames = this.activeSeries().map((s) => s.label).join(', ');
    const range = this.points.length
      ? `${this.monthLabel(this.points[0]?.month)} to ${this.monthLabel(this.points[this.points.length - 1]?.month)}`
      : '';
    return `Bar chart showing ${seriesNames} per month, ${range}. Full values are in the table below the chart.`;
  }

  private buildChartData(): void {
    const series = this.activeSeries();
    this.chartData = {
      labels: this.points.map((p) => this.monthLabel(p.month)),
      datasets: series.map((s) => ({
        label: s.label,
        data: this.points.map((p) => this.valueFor(p, s.key)),
        backgroundColor: barGradient(s.color),
        borderColor: lighten(s.color, 0.35),
        borderWidth: 1,
        borderRadius: 4,
        maxBarThickness: 28,
      })),
    };
  }
}
