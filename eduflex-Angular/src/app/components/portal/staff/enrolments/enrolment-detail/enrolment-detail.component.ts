import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Client, EducationPartnerDto, UserSummaryDto, UserFilterDto } from '@services/api.services';
import { EnrolmentService } from '@services/enrolment.service';
import { FinancialRecordService } from '@services/financial-record.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { ModalComponent } from '@generic/modal/modal.component';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { Enrolment } from '../../../../../models/enrolment';
import { VisaProcessTabComponent } from './tabs/visa-process-tab/visa-process-tab.component';
import { DocumentsTabComponent } from './tabs/documents-tab/documents-tab.component';
import { CommunicationTabComponent } from './tabs/communication-tab/communication-tab.component';
import { AuditTabComponent } from './tabs/audit-tab/audit-tab.component';

type EnrolmentTab = 'visa' | 'documents' | 'communication' | 'audit';

@Component({
  selector: 'app-enrolment-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, VisaProcessTabComponent, DocumentsTabComponent, CommunicationTabComponent, AuditTabComponent],
  templateUrl: './enrolment-detail.component.html',
  styleUrls: ['./enrolment-detail.component.css']
})
export class EnrolmentDetailComponent implements OnInit {
  enrolmentId = '';
  enrolment: Enrolment | null = null;
  isLoading = true;
  activeTab: EnrolmentTab = 'visa';

  permissions!: ModulePermissions;
  currentUserId = '';
  isOwner = false;

  partners: EducationPartnerDto[] = [];

  showReassignModal = false;
  staffOptions: UserSummaryDto[] = [];
  selectedNewOwnerId = '';
  isReassigning = false;

  financePermissions!: ModulePermissions;
  financialRecordId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private apiClient: Client,
    private enrolmentService: EnrolmentService,
    private financialRecordService: FinancialRecordService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasEnrolmentsPermission();
    this.financePermissions = this.authHelper.hasFinancePermission();
    this.currentUserId = this.authHelper.getCurrentUser()?.id ?? '';
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.enrolmentId = id;

    this.loadPartners();
    this.loadEnrolment();
  }

  private loadPartners(): void {
    this.apiClient.educationPartnersAll().subscribe({
      next: (partners) => { this.partners = partners; },
      error: () => {}
    });
  }

  // isLoading only gates the *ngIf that mounts the tab components — it must stay false
  // across refreshes triggered by a child's (changed) event (after a save/upload/send),
  // or every one of those actions would destroy and recreate all four tabs and wipe out
  // whatever draft state was sitting in the others. Only the very first load shows the
  // spinner and gates mounting; later calls just swap in a fresh `enrolment` reference.
  loadEnrolment(): void {
    const isInitialLoad = this.enrolment === null;
    if (isInitialLoad) { this.isLoading = true; }

    this.enrolmentService.get(this.enrolmentId).subscribe({
      next: (enrolment) => {
        this.enrolment = enrolment;
        this.isOwner = enrolment.ownerUserId === this.currentUserId;
        if (isInitialLoad) { this.isLoading = false; }
        this.loadFinancialRecordLink();
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this enrolment.');
        this.goBack();
      }
    });
  }

  switchTab(tab: EnrolmentTab): void {
    this.activeTab = tab;
  }

  // A financial record only exists once VISA Outcome has been completed (Granted or
  // Refused) — a 404 here just means "not created yet", not an error, so it's swallowed
  // silently and the button below simply doesn't render.
  private loadFinancialRecordLink(): void {
    if (!this.financePermissions.view || !this.enrolment) { return; }
    if (this.enrolment.status !== 'VisaSuccess' && this.enrolment.status !== 'VisaFail') { return; }

    this.financialRecordService.getByEnrolmentId(this.enrolment.id).subscribe({
      next: (record) => { this.financialRecordId = record.id; },
      error: () => { this.financialRecordId = null; }
    });
  }

  goToFinancialRecord(): void {
    if (this.financialRecordId) {
      this.router.navigate(['/staff-portal/financial-records', this.financialRecordId]);
    }
  }

  // ----- Reassign -----

  openReassignModal(): void {
    if (!this.permissions.reassign) {
      this.notificationService.error('You do not have permission to reassign enrolments.');
      return;
    }
    this.selectedNewOwnerId = '';
    this.showReassignModal = true;
    this.apiClient.searchUsers(new UserFilterDto({ pageNumber: 1, pageSize: 100, isActive: true })).subscribe({
      next: (result) => {
        this.staffOptions = (result.items ?? []).filter(u => u.id !== this.enrolment?.ownerUserId && u.roleName !== 'Student');
      },
      error: () => {}
    });
  }

  confirmReassign(): void {
    if (!this.selectedNewOwnerId) return;
    this.isReassigning = true;
    this.enrolmentService.reassign(this.enrolmentId, this.selectedNewOwnerId).subscribe({
      next: () => {
        this.isReassigning = false;
        this.showReassignModal = false;
        this.notificationService.success('Enrolment reassigned.');
        this.loadEnrolment();
      },
      error: (err) => {
        this.isReassigning = false;
        this.notificationService.error(extractHttpErrorMessage(err, 'Could not reassign this enrolment.'));
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/enrolments']);
  }
}
