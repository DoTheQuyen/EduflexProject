import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { VisaProcessTemplateService } from '@services/visa-process-template.service';
import { PractitionerTagService } from '@services/practitioner-tag.service';
import { AuthHelperService } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import {
  TemplateStatus,
  StepFieldInputType,
  StepPreconditionType,
  STEP_FIELD_INPUT_TYPES,
  STEP_PRECONDITION_TYPES,
  VisaProcessStepDefinition,
  StepFieldDefinition,
  StepPrecondition,
  ProcessStepHint,
  newStepDefinition,
  newFieldDefinition,
  newPrecondition
} from '@app/models/visa-process-template';
import { PractitionerTag } from '@app/models/practitioner-tag';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

@Component({
  selector: 'app-visa-process-template-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './visa-process-template-edit.component.html',
  styleUrls: ['./visa-process-template-edit.component.css']
})
export class VisaProcessTemplateEditComponent implements OnInit {
  readonly fieldInputTypes: StepFieldInputType[] = STEP_FIELD_INPUT_TYPES;
  readonly preconditionTypes: StepPreconditionType[] = STEP_PRECONDITION_TYPES;

  isEditMode = false;
  templateId: string | null = null;
  isLoading = false;
  isSaving = false;

  name = '';
  country = 'AU';
  category = '';
  description = '';
  status: TemplateStatus = 'Active';
  isDefaultForCountry = false;
  steps: VisaProcessStepDefinition[] = [newStepDefinition(0)];

  practitionerTags: PractitionerTag[] = [];

  // New-evidence-category / new-allowed-value / new-hint text inputs — kept per-row by a
  // string key rather than a form control per row, matching this module's plain-array
  // style. Allowed-value inputs are keyed "stepIndex-preconditionIndex" since a step can
  // have more than one FieldValueIn precondition on screen at once.
  newEvidenceCategoryInput: Record<number, string> = {};
  newAllowedValueInput: Record<string, string> = {};
  newHintTextInput: Record<number, string> = {};

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private templateService: VisaProcessTemplateService,
    private tagService: PractitionerTagService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.tagService.getAll().subscribe({ next: (tags) => (this.practitionerTags = tags) });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { return; }

    this.isEditMode = true;
    this.templateId = id;
    this.isLoading = true;
    this.templateService.getById(id).subscribe({
      next: (template) => {
        this.name = template.name;
        this.country = template.country;
        this.category = template.category;
        this.description = template.description ?? '';
        this.status = template.status;
        this.isDefaultForCountry = template.isDefaultForCountry;
        this.steps = template.steps.length > 0
          ? [...template.steps].sort((a, b) => a.order - b.order)
          : [newStepDefinition(0)];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this template.');
        this.goBack();
      }
    });
  }

  activeTags(currentId?: string | null): PractitionerTag[] {
    return this.practitionerTags.filter((t) => t.active || t.id === currentId);
  }

  needsOptions(type: StepFieldInputType): boolean {
    return type === 'Select';
  }

  // ----- Steps -----
  addStep(): void {
    this.steps.push(newStepDefinition(this.steps.length));
  }

  removeStep(index: number): void {
    this.steps.splice(index, 1);
  }

  moveStep(index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= this.steps.length) return;
    [this.steps[index], this.steps[target]] = [this.steps[target], this.steps[index]];
  }

  // ----- Fields -----
  addField(step: VisaProcessStepDefinition): void {
    step.fields.push(newFieldDefinition());
  }

  removeField(step: VisaProcessStepDefinition, index: number): void {
    step.fields.splice(index, 1);
  }

  addFieldOption(field: StepFieldDefinition): void {
    field.options.push('');
  }

  removeFieldOption(field: StepFieldDefinition, index: number): void {
    field.options.splice(index, 1);
  }

  // ----- Required evidence categories -----
  addEvidenceCategory(step: VisaProcessStepDefinition, index: number): void {
    const value = (this.newEvidenceCategoryInput[index] ?? '').trim();
    if (!value) return;
    step.requiredEvidenceCategories.push(value);
    this.newEvidenceCategoryInput[index] = '';
  }

  removeEvidenceCategory(step: VisaProcessStepDefinition, categoryIndex: number): void {
    step.requiredEvidenceCategories.splice(categoryIndex, 1);
  }

  // ----- Preconditions -----
  addPrecondition(step: VisaProcessStepDefinition): void {
    step.preconditions.push(newPrecondition());
  }

  removePrecondition(step: VisaProcessStepDefinition, index: number): void {
    step.preconditions.splice(index, 1);
  }

  allowedValueKey(stepIndex: number, preconditionIndex: number): string {
    return `${stepIndex}-${preconditionIndex}`;
  }

  addAllowedValue(precondition: StepPrecondition, key: string): void {
    const value = (this.newAllowedValueInput[key] ?? '').trim();
    if (!value) return;
    precondition.allowedValues.push(value);
    this.newAllowedValueInput[key] = '';
  }

  removeAllowedValue(precondition: StepPrecondition, index: number): void {
    precondition.allowedValues.splice(index, 1);
  }

  // ----- Hints -----
  addHint(step: VisaProcessStepDefinition, index: number): void {
    const text = (this.newHintTextInput[index] ?? '').trim();
    if (!text) return;

    const user = this.authHelper.getCurrentUser();
    const hint: ProcessStepHint = {
      id: '',
      text,
      authorUserId: user?.id ?? null,
      authorName: user ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() : null,
      createdAt: new Date().toISOString(),
      pinned: false
    };
    step.hints = [hint, ...step.hints];
    this.newHintTextInput[index] = '';
  }

  togglePinned(hint: ProcessStepHint): void {
    hint.pinned = !hint.pinned;
  }

  // ----- Save -----
  get isValid(): boolean {
    if (!this.name.trim() || !this.country.trim() || !this.category.trim() || this.steps.length === 0) return false;
    return this.steps.every((s) =>
      s.key.trim().length > 0 &&
      s.label.trim().length > 0 &&
      s.fields.every((f) =>
        f.fieldKey.trim().length > 0 &&
        f.label.trim().length > 0 &&
        (!this.needsOptions(f.inputType) || f.options.filter((o) => o.trim()).length >= 2)
      )
    );
  }

  save(): void {
    if (!this.isValid) {
      this.notificationService.error('Every step needs a key and a label, every field needs a key and a label, and Select fields need at least two options.');
      return;
    }

    const payload = {
      name: this.name.trim(),
      country: this.country.trim(),
      category: this.category.trim(),
      description: this.description.trim() || undefined,
      status: this.status,
      isDefaultForCountry: this.isDefaultForCountry,
      steps: this.steps.map((s, i) => ({ ...s, order: i }))
    };

    this.isSaving = true;

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
    this.notificationService.success(this.isEditMode ? 'Template updated.' : 'Template created.');
    this.goBack();
  }

  private onSaveError(err: unknown): void {
    this.isSaving = false;
    this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this template.'));
  }

  trackByIndex(index: number): number {
    return index;
  }

  goBack(): void {
    this.router.navigate(['/staff-portal/visa-process-templates']);
  }
}
