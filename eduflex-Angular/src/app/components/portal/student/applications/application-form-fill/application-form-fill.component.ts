import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NotificationService } from '@services/notification.service';
import { EnrolmentService } from '@services/enrolment.service';
import { ModalComponent } from '@generic/modal/modal.component';
import { FormAnswerEditorComponent } from '@generic/form-answer-editor/form-answer-editor.component';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { formatDateTime } from '@app/shared/utils/date-time.util';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { EnrolmentFormResponse, FormAnswer, formResponseStatusBadgeClass } from '@app/models/dynamic-form';

@Component({
  selector: 'app-application-form-fill',
  standalone: true,
  imports: [CommonModule, RouterModule, ModalComponent, FormAnswerEditorComponent, FormPrintPreviewComponent],
  templateUrl: './application-form-fill.component.html',
  styleUrls: ['./application-form-fill.component.css']
})
export class ApplicationFormFillComponent implements OnInit {
  applicationId: string | null = null;
  formId: string | null = null;

  isLoading = false;
  enrolmentId: string | null = null;
  form: EnrolmentFormResponse | null = null;
  fillAnswers: FormAnswer[] = [];

  isSavingDraft = false;
  isSubmitting = false;
  showFinalizeConfirm = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private notification: NotificationService,
    private enrolmentService: EnrolmentService
  ) {}

  ngOnInit(): void {
    this.applicationId = this.route.snapshot.paramMap.get('id');
    this.formId = this.route.snapshot.paramMap.get('formId');
    if (!this.applicationId || !this.formId) {
      this.router.navigate(['/student-portal/application']);
      return;
    }
    this.loadForm();
  }

  private loadForm(): void {
    this.isLoading = true;
    this.enrolmentService.getMyForms(this.applicationId!).subscribe({
      next: (result) => {
        this.enrolmentId = result.enrolmentId;
        const form = result.forms.find(f => f.id === this.formId);
        if (!form) {
          this.notification.error('This form could not be found.');
          this.isLoading = false;
          this.router.navigate(['/student-portal/application', this.applicationId]);
          return;
        }
        this.form = form;
        this.fillAnswers = form.answers.map(a => ({ ...a, selectedOptions: [...a.selectedOptions] }));
        this.isLoading = false;
      },
      error: () => {
        this.notification.error('Could not load this form.');
        this.isLoading = false;
        this.router.navigate(['/student-portal/application', this.applicationId]);
      }
    });
  }

  get isFormLocked(): boolean {
    return this.form?.status === 'Responded';
  }

  formStatusBadgeClass(): string {
    return this.form ? formResponseStatusBadgeClass(this.form.status) : '';
  }

  formatDate(value: any): string {
    return formatDateTime(value, 'dd MMM yyyy');
  }

  backToApplication(): void {
    this.router.navigate(['/student-portal/application', this.applicationId]);
  }

  saveFormDraft(): void {
    if (!this.enrolmentId || !this.form) return;
    this.isSavingDraft = true;
    this.enrolmentService.saveFormDraft(this.enrolmentId, this.form.id, this.fillAnswers).subscribe({
      next: () => {
        this.isSavingDraft = false;
        this.notification.success('Draft saved.');
        this.loadForm();
      },
      error: (err) => {
        this.isSavingDraft = false;
        this.notification.error(extractHttpErrorMessage(err, 'Could not save your draft.'));
      }
    });
  }

  openFinalizeConfirm(): void {
    this.showFinalizeConfirm = true;
  }

  confirmSubmitForm(): void {
    if (!this.enrolmentId || !this.form) return;
    this.isSubmitting = true;
    this.enrolmentService.submitForm(this.enrolmentId, this.form.id, this.fillAnswers).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showFinalizeConfirm = false;
        this.notification.success('Form submitted.');
        this.router.navigate(['/student-portal/application', this.applicationId]);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notification.error(extractHttpErrorMessage(err, 'Could not submit this form — check every required question is answered.'));
      }
    });
  }
}