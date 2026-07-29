import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Client, EducationPartnerDto, CourseDto } from '@services/api.services';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { Enrolment, EnrolmentDocument } from '../../../../../../../models/enrolment';

@Component({
  selector: 'app-summary-tab',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './summary-tab.component.html',
  styleUrls: ['./summary-tab.component.css']
})
export class SummaryTabComponent implements OnChanges {
  @Input() enrolment: Enrolment | null = null;

  partner: EducationPartnerDto | null = null;
  course: CourseDto | null = null;

  constructor(private apiClient: Client) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['enrolment'] || !this.enrolment) return;

    if (this.enrolment.educationPartnerId) {
      this.apiClient.educationPartnersGET(this.enrolment.educationPartnerId).subscribe({
        next: (partner) => {
          this.partner = partner;
          this.course = (partner.courses ?? []).find(c => c.id === this.enrolment?.courseId) ?? null;
        },
        error: () => {}
      });
    }
  }

  get visaDocument(): EnrolmentDocument | undefined {
    return this.enrolment?.documents.find(d => d.category === 'VisaGranted');
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy') : '—';
  }
}
