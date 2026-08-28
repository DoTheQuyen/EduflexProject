import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Client, PersonType, StudentAccountDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { EnrolmentService } from '@services/enrolment.service';
import { environment } from '../../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Enrolment, EnrolmentDocument, documentCategoryLabel } from '../../../../../models/enrolment';
import { Application, ACTIVE_APPLICATION_STATUSES } from '../../../../../models/application';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

interface HistoryEntry {
  enrolment: Enrolment;
  hasVisaOutcome: boolean;
  enrolmentOpen: boolean;
  visaOutcomeOpen: boolean;
}

const TERMINAL_ENROLMENT_STATUSES = ['Cancel', 'VisaFail', 'Completed'];
const VISA_OUTCOME_CATEGORIES = ['VisaGranted', 'VisaPaymentReceipt'];

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './student-detail.component.html',
  styleUrls: ['./student-detail.component.css']
})
export class StudentDetailComponent implements OnInit {
  student: StudentAccountDto | null = null;
  applications: Application[] = [];
  enrolments: Enrolment[] = [];
  history: HistoryEntry[] = [];

  isLoading = false;
  permissions: ModulePermissions;

  private readonly applicationsUrl = `${environment.apiClientUrl}/api/Applications`;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasStudentsPermission();
  }

  get isCustomer(): boolean {
    return this.student?.type === PersonType.Customer;
  }

  get typeLabel(): string {
    return this.isCustomer ? 'Customer' : 'Student';
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.load(id);
  }

  private load(id: string): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view contacts.');
      this.router.navigate(['/staff-portal/contacts']);
      return;
    }

    this.isLoading = true;
    this.apiClient.studentsGET(id).subscribe({
      next: (student) => {
        this.student = student;
        this.loadApplicationsAndEnrolments(student.id!, student.userId!);
      },
      error: (err) => {
        this.isLoading = false;
        this.notificationService.error(extractApiErrorMessage(err, 'Could not load this record.'));
      }
    });
  }

  private loadApplicationsAndEnrolments(studentId: string, studentUserId: string): void {
    forkJoin({
      applications: this.http.get<Application[]>(`${this.applicationsUrl}/by-student/${studentId}`),
      enrolments: this.enrolmentService.getByStudent(studentUserId)
    }).subscribe({
      next: ({ applications, enrolments }) => {
        this.applications = applications;
        this.enrolments = enrolments;
        this.history = enrolments
          .slice()
          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
          .map(enrolment => ({
            enrolment,
            hasVisaOutcome: enrolment.status === 'VisaSuccess' || enrolment.status === 'VisaFail',
            enrolmentOpen: false,
            visaOutcomeOpen: false
          }));
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.notificationService.error(extractApiErrorMessage(err, "Could not load this student's applications and enrolments."));
      }
    });
  }

  get activeApplications(): Application[] {
    return this.applications.filter(a => ACTIVE_APPLICATION_STATUSES.includes(a.status));
  }

  get totalDocumentCount(): number {
    return this.enrolments.reduce((sum, e) => sum + e.documents.length, 0);
  }

  get hasActiveEnrolment(): boolean {
    return this.enrolments.some(e => !TERMINAL_ENROLMENT_STATUSES.includes(e.status));
  }

  get hasActiveApplicationOrEnrolment(): boolean {
    return this.activeApplications.length > 0 || this.hasActiveEnrolment;
  }

  enrolmentDocuments(enrolment: Enrolment): EnrolmentDocument[] {
    return enrolment.documents.filter(d => !VISA_OUTCOME_CATEGORIES.includes(d.category as string));
  }

  visaOutcomeDocuments(enrolment: Enrolment): EnrolmentDocument[] {
    return enrolment.documents.filter(d => VISA_OUTCOME_CATEGORIES.includes(d.category as string));
  }

  categoryLabel(category?: string): string {
    return documentCategoryLabel(category);
  }

  toggleEnrolment(entry: HistoryEntry): void {
    entry.enrolmentOpen = !entry.enrolmentOpen;
  }

  toggleVisaOutcome(entry: HistoryEntry): void {
    entry.visaOutcomeOpen = !entry.visaOutcomeOpen;
  }

  deactivate(): void {
    if (!this.student?.id) return;

    let message = `Deactivate ${this.student.firstName} ${this.student.lastName}? They will no longer be able to log in.`;
    if (this.hasActiveApplicationOrEnrolment) {
      message += '\n\nThis student has an active application or enrolment in progress. Deactivating will also remove all of their enrolment documents (payment and visa-payment receipts are kept for Finance).';
    }

    if (!window.confirm(message)) return;

    this.apiClient.deactivate(this.student.id).subscribe({
      next: () => {
        this.notificationService.success(`${this.typeLabel} deactivated.`);
        this.load(this.student!.id!);
      },
      error: (err) => {
        this.notificationService.error(extractApiErrorMessage(err, 'Could not deactivate this record.'));
      }
    });
  }

  reactivate(): void {
    if (!this.student?.id) return;
    if (!window.confirm(`Reactivate ${this.student.firstName} ${this.student.lastName}?`)) return;

    this.apiClient.reactivate(this.student.id).subscribe({
      next: () => {
        this.notificationService.success(`${this.typeLabel} reactivated.`);
        this.load(this.student!.id!);
      },
      error: (err) => {
        this.notificationService.error(extractApiErrorMessage(err, 'Could not reactivate this record.'));
      }
    });
  }
}
