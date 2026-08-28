import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Client, StatusBreakdownDto } from '../../../../../services/api.services';
import { StatusBreakdownChartComponent } from '../status-breakdown-chart/status-breakdown-chart.component';

interface ModuleCard {
  key: keyof StatusBreakdownDto;
  title: string;
  color: string;
}

const MODULES: ModuleCard[] = [
  { key: 'enquiry', title: 'Enquiries', color: '#16233a' },
  { key: 'application', title: 'Applications', color: '#b8862f' },
  { key: 'enrolment', title: 'Enrolments', color: '#1f7a5c' },
  { key: 'migrationCase', title: 'Migration Cases', color: '#d2530c' },
];

@Component({
  selector: 'app-dashboard-status-breakdown',
  standalone: true,
  imports: [CommonModule, ProgressSpinner, StatusBreakdownChartComponent],
  templateUrl: './status-breakdown.component.html',
  styleUrls: ['./status-breakdown.component.css'],
})
export class StatusBreakdownComponent implements OnInit {
  readonly modules = MODULES;

  loading = true;
  error = false;
  breakdown: StatusBreakdownDto | null = null;

  constructor(private client: Client) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = false;
    this.client.statusBreakdown().subscribe({
      next: (result) => {
        this.breakdown = result;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.error = true;
      },
    });
  }

  itemsFor(module: ModuleCard) {
    return this.breakdown ? (this.breakdown[module.key] as any) : null;
  }
}
