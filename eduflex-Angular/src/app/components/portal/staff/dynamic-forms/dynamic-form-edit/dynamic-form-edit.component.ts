import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DynamicFormTemplateService } from '@services/dynamic-form-template.service';
import { NotificationService } from '@services/notification.service';
import { FormPrintPreviewComponent } from '@generic/form-print-preview/form-print-preview.component';
import { AnswerType, ANSWER_TYPE_LABELS, FormQuestion, RICH_TEXT_MAX_LENGTH, TemplateStatus, newQuestion } from '@app/models/dynamic-form';
import { VISA_STEP_ORDER, VISA_STEP_LABELS } from '@app/models/enrolment';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

const ANSWER_TYPES: AnswerType[] = ['RichText', 'YesNo', 'SingleSelect', 'MultiSelect'];

@Component({
  selector: 'app-dynamic-form-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, FormPrintPreviewComponent],
  templateUrl: './dynamic-form-edit.component.html',
  styleUrls: ['./dynamic-form-edit.component.css']
})
export class DynamicFormEditComponent implements OnInit {
  readonly answerTypes = ANSWER_TYPES;
  readonly answerTypeLabels = ANSWER_TYPE_LABELS;
  // Enrolment's fixed step keys, offered as datalist suggestions only — see
  // models/dynamic-form.ts's comment on why boundStepKey isn't restricted to this list.
  readonly stepOrder = VISA_STEP_ORDER;
  readonly stepLabels = VISA_STEP_LABELS;
  readonly defaultRichTextMaxLength = RICH_TEXT_MAX_LENGTH;

  isEditMode = false;
  templateId: string | null = null;
  isLoading = false;
  isSaving = false;

  name = '';
  description = '';
  status: TemplateStatus = 'Active';
  boundStepKey = '';
  questions: FormQuestion[] = [newQuestion(0)];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private templateService: DynamicFormTemplateService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { return; }

    this.isEditMode = true;
    this.templateId = id;
    this.isLoading = true;
    this.templateService.getById(id).subscribe({
      next: (template) => {
        this.name = template.name;
        this.description = template.description ?? '';
        this.status = template.status;
        this.boundStepKey = template.boundStepKey ?? '';
        this.questions = template.questions.length > 0
          ? [...template.questions].sort((a, b) => a.order - b.order)
          : [newQuestion(0)];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this form.');
        this.goBack();
      }
    });
  }

  needsOptions(type: AnswerType): boolean {
    return type === 'SingleSelect' || type === 'MultiSelect';
  }

  addQuestion(): void {
    this.questions.push(newQuestion(this.questions.length));
  }

  removeQuestion(index: number): void {
    this.questions.splice(index, 1);
  }

  moveQuestion(index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= this.questions.length) return;
    [this.questions[index], this.questions[target]] = [this.questions[target], this.questions[index]];
  }

  addOption(question: FormQuestion): void {
    question.options.push('');
  }

  removeOption(question: FormQuestion, optionIndex: number): void {
    question.options.splice(optionIndex, 1);
  }

  get isValid(): boolean {
    if (!this.name.trim() || this.questions.length === 0) return false;
    return this.questions.every(q =>
      q.questionText.trim().length > 0 &&
      (!this.needsOptions(q.answerType) || q.options.filter(o => o.trim()).length >= 2)
    );
  }

  save(): void {
    if (!this.isValid) {
      this.notificationService.error('Give the form a name, and make sure every question has text (select questions need at least two options).');
      return;
    }

    const payload = {
      name: this.name.trim(),
      description: this.description.trim() || undefined,
      status: this.status,
      boundStepKey: this.boundStepKey || null,
      questions: this.questions.map((q, i) => ({ ...q, order: i }))
    };

    this.isSaving = true;

    // Kept as two branches (rather than a shared `request$` union) — TypeScript can't
    // unify Observable<DynamicFormTemplate> (create) and Observable<boolean> (update)
    // into a single callable .subscribe() across the ternary.
    if (this.isEditMode && this.templateId) {
      this.templateService.update(this.templateId, payload).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err: unknown) => this.onSaveError(err)
      });
    } else {
      this.templateService.create(payload).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err: unknown) => this.onSaveError(err)
      });
    }
  }

  private onSaveSuccess(): void {
    this.isSaving = false;
    this.notificationService.success(this.isEditMode ? 'Form updated.' : 'Form created.');
    this.goBack();
  }

  private onSaveError(err: unknown): void {
    this.isSaving = false;
    this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this form.'));
  }

  trackByIndex(index: number): number {
    return index;
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/dynamic-forms']);
  }
}
