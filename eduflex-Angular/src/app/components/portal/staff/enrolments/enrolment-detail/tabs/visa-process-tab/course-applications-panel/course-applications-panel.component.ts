import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Client, EducationPartnerDto, CourseDto, AddCourseApplicationDto, SetCourseApplicationStatusDto, UpdateCourseApplicationDetailsDto } from '@services/api.services';
import { ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import {
  Enrolment, CourseApplication, CourseApplicationStatus,
  COURSE_APPLICATION_STATUS_LABELS, courseApplicationStatusBadgeClass
} from '../../../../../../../../models/enrolment';
import { StepEvidenceSectionComponent } from '../step-evidence-section/step-evidence-section.component';

// The Enrolment Form step's nested "Course applications" sub-panels — a student can
// pursue several courses/universities at once under one Enrolment, each gets its own
// expandable row here. Extracted out of visa-process-tab.component so that component
// stays a manageable size; the parent only needs effectiveMaxApplications (its own
// "Number applications allowed" header control feeds the same limit).
@Component({
  selector: 'app-course-applications-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, StepEvidenceSectionComponent],
  templateUrl: './course-applications-panel.component.html',
  styleUrls: ['./course-applications-panel.component.css']
})
export class CourseApplicationsPanelComponent {
  @Input({ required: true }) enrolment!: Enrolment;
  @Input() partners: EducationPartnerDto[] = [];
  @Input() isOwner = false;
  @Input({ required: true }) permissions!: ModulePermissions;
  @Input({ required: true }) effectiveMaxApplications!: number;
  @Output() changed = new EventEmitter<void>();

  readonly courseApplicationStatusLabels = COURSE_APPLICATION_STATUS_LABELS;

  openCourseApplicationId: string | null = null;
  showAddCourseApplicationForm = false;
  newCourseEducationPartnerId = '';
  newCourseCourseId = '';
  isAddingCourseApplication = false;
  isSavingCourseApplicationStatus: Record<string, boolean> = {};
  editingCourseApplicationId: string | null = null;
  isSavingCourseApplicationDetails: Record<string, boolean> = {};
  caDraft: {
    intake: string; studyMode: string; campus: string;
    commencementDate: string; expectedCompletionDate: string; actualCommencementDate: string;
    tuitionFee: number | null; notes: string;
  } = { intake: '', studyMode: '', campus: '', commencementDate: '', expectedCompletionDate: '', actualCommencementDate: '', tuitionFee: null, notes: '' };

  constructor(
    private apiClient: Client,
    private notificationService: NotificationService
  ) {}

  private canEdit(): boolean {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit enrolments.');
      return false;
    }
    if (!this.isOwner) {
      this.notificationService.error('Only the staff member who owns this enrolment can edit it. Ask a manager to reassign it to you.');
      return false;
    }
    return true;
  }

  formatDate(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy HH:mm') : '';
  }

  formatDateOnly(value: string | undefined): string {
    return value ? formatDateTime(value, 'dd/MM/yyyy') : 'unknown date';
  }

  hasOfferAttached(courseApplication: CourseApplication): boolean {
    return this.enrolment.documents.some(d => d.courseApplicationId === courseApplication.id && d.category === 'UniOffer');
  }

  get activeCourseApplicationCount(): number {
    return this.enrolment.courseApplications.filter(c => c.status !== 'Withdrawn').length;
  }

  get canAddCourseApplication(): boolean {
    return this.activeCourseApplicationCount < this.effectiveMaxApplications;
  }

  courseApplicationPartnerName(educationPartnerId: string): string {
    return this.partners.find(p => p.id === educationPartnerId)?.name ?? 'Unknown university';
  }

  courseApplicationCourseName(educationPartnerId: string, courseId: string): string {
    const partner = this.partners.find(p => p.id === educationPartnerId);
    return partner?.courses?.find(c => c.id === courseId)?.courseName ?? 'Unknown course';
  }

  coursesForPartner(educationPartnerId: string): CourseDto[] {
    return this.partners.find(p => p.id === educationPartnerId)?.courses ?? [];
  }

  courseApplicationBadgeClass(status: CourseApplicationStatus): string {
    return courseApplicationStatusBadgeClass(status);
  }

  toggleCourseApplicationPanel(courseApplicationId: string): void {
    this.openCourseApplicationId = this.openCourseApplicationId === courseApplicationId ? null : courseApplicationId;
  }

  toggleAddCourseApplicationForm(): void {
    if (!this.canEdit()) return;
    this.showAddCourseApplicationForm = !this.showAddCourseApplicationForm;
    this.newCourseEducationPartnerId = '';
    this.newCourseCourseId = '';
  }

  addCourseApplication(): void {
    if (!this.canEdit() || !this.newCourseEducationPartnerId || !this.newCourseCourseId) return;

    this.isAddingCourseApplication = true;
    this.apiClient.courseApplications(this.enrolment.id, new AddCourseApplicationDto({
      educationPartnerId: this.newCourseEducationPartnerId,
      courseId: this.newCourseCourseId
    })).subscribe({
      next: () => {
        this.isAddingCourseApplication = false;
        this.showAddCourseApplicationForm = false;
        this.notificationService.success('Course application added.');
        this.changed.emit();
      },
      error: (err) => {
        this.isAddingCourseApplication = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not add this course application.'));
      }
    });
  }

  setCourseApplicationStatus(courseApplication: CourseApplication, newStatus: CourseApplicationStatus): void {
    if (!this.canEdit()) return;
    this.isSavingCourseApplicationStatus[courseApplication.id] = true;
    this.apiClient.statusPUT2(this.enrolment.id, courseApplication.id, new SetCourseApplicationStatusDto({ status: newStatus })).subscribe({
      next: () => {
        this.isSavingCourseApplicationStatus[courseApplication.id] = false;
        this.notificationService.success('Status updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingCourseApplicationStatus[courseApplication.id] = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not update this course application\'s status.'));
      }
    });
  }

  toggleEditCourseApplicationDetails(courseApplication: CourseApplication): void {
    if (!this.canEdit()) return;
    this.editingCourseApplicationId = courseApplication.id;
    this.caDraft = {
      intake: courseApplication.intake ?? '',
      studyMode: courseApplication.studyMode ?? '',
      campus: courseApplication.campus ?? '',
      commencementDate: courseApplication.commencementDate ? courseApplication.commencementDate.substring(0, 10) : '',
      expectedCompletionDate: courseApplication.expectedCompletionDate ? courseApplication.expectedCompletionDate.substring(0, 10) : '',
      actualCommencementDate: courseApplication.actualCommencementDate ? courseApplication.actualCommencementDate.substring(0, 10) : '',
      tuitionFee: courseApplication.tuitionFee ?? null,
      notes: courseApplication.notes ?? ''
    };
  }

  cancelEditCourseApplicationDetails(): void {
    this.editingCourseApplicationId = null;
  }

  saveCourseApplicationDetails(courseApplication: CourseApplication): void {
    if (!this.canEdit()) return;
    this.isSavingCourseApplicationDetails[courseApplication.id] = true;
    this.apiClient.details(this.enrolment.id, courseApplication.id, new UpdateCourseApplicationDetailsDto({
      intake: this.caDraft.intake || undefined,
      studyMode: this.caDraft.studyMode || undefined,
      campus: this.caDraft.campus || undefined,
      commencementDate: this.caDraft.commencementDate ? new Date(this.caDraft.commencementDate) : undefined,
      expectedCompletionDate: this.caDraft.expectedCompletionDate ? new Date(this.caDraft.expectedCompletionDate) : undefined,
      actualCommencementDate: this.caDraft.actualCommencementDate ? new Date(this.caDraft.actualCommencementDate) : undefined,
      tuitionFee: this.caDraft.tuitionFee ?? undefined,
      notes: this.caDraft.notes || undefined
    })).subscribe({
      next: () => {
        this.isSavingCourseApplicationDetails[courseApplication.id] = false;
        this.editingCourseApplicationId = null;
        this.notificationService.success('Course application details updated.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingCourseApplicationDetails[courseApplication.id] = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not update this course application.'));
      }
    });
  }

  finalizeCourseApplication(courseApplication: CourseApplication): void {
    if (!this.canEdit() || courseApplication.status !== 'Offered' || !this.hasOfferAttached(courseApplication)) return;

    const otherActiveCount = this.enrolment.courseApplications.filter(c => c.id !== courseApplication.id && c.status !== 'Withdrawn').length;
    const offerDate = this.formatDateOnly(courseApplication.offerAppliedDate);
    const message = `Finalize this course application (offer applied ${offerDate})?\n\n`
      + (otherActiveCount > 0
        ? `Every other course application on this enrolment (${otherActiveCount}) will be withdrawn, and their attached offer letters will be permanently removed from the system.`
        : `This is the only active course application, so nothing else will be affected.`);
    if (!confirm(message)) return;

    this.isSavingCourseApplicationStatus[courseApplication.id] = true;
    this.apiClient.finalize2(this.enrolment.id, courseApplication.id).subscribe({
      next: () => {
        this.isSavingCourseApplicationStatus[courseApplication.id] = false;
        this.notificationService.success('Course application finalized — other applications withdrawn.');
        this.changed.emit();
      },
      error: (err) => {
        this.isSavingCourseApplicationStatus[courseApplication.id] = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not finalize this course application.'));
      }
    });
  }
}
