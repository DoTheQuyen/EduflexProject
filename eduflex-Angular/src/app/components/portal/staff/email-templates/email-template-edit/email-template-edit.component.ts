import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { QuillModule } from 'ngx-quill';
import { EmailTemplateService } from '@services/email-template.service';
import { NotificationService } from '@services/notification.service';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';
import { RICH_TEXT_QUILL_MODULES } from '@app/shared/utils/quill-toolbar.util';

@Component({
  selector: 'app-email-template-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, QuillModule],
  templateUrl: './email-template-edit.component.html',
  styleUrls: ['./email-template-edit.component.css']
})
export class EmailTemplateEditComponent implements OnInit {
  readonly quillModules = RICH_TEXT_QUILL_MODULES;

  isEditMode = false;
  templateId: string | null = null;
  isLoading = false;
  isSaving = false;

  key = '';
  name = '';
  subject = '';
  body = '';
  isSystemDefault = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private templateService: EmailTemplateService,
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
        this.key = template.key;
        this.name = template.name;
        this.subject = template.subject;
        this.body = template.body;
        this.isSystemDefault = template.isSystemDefault;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this template.');
        this.goBack();
      }
    });
  }

  get isValid(): boolean {
    if (!this.name.trim() || !this.subject.trim() || !this.body.trim()) return false;
    return this.isEditMode || /^[a-z0-9-]+$/.test(this.key.trim());
  }

  save(): void {
    if (!this.isValid) {
      this.notificationService.error(this.isEditMode
        ? 'Give the template a name, subject and body.'
        : 'Give the template a key (lowercase letters, numbers and hyphens only), name, subject and body.');
      return;
    }

    this.isSaving = true;

    // Kept as two branches (rather than a shared `request$` union) — TypeScript can't
    // unify Observable<EmailTemplate> (create) and Observable<boolean> (update) into a
    // single callable .subscribe() across the ternary, same reasoning as
    // DynamicFormEditComponent.save().
    if (this.isEditMode && this.templateId) {
      this.templateService.update(this.templateId, {
        name: this.name.trim(),
        subject: this.subject.trim(),
        body: this.body
      }).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err: unknown) => this.onSaveError(err)
      });
    } else {
      this.templateService.create({
        key: this.key.trim(),
        name: this.name.trim(),
        subject: this.subject.trim(),
        body: this.body
      }).subscribe({
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

  goBack(): void {
    this.router.navigate(['/staff-portal/email-templates']);
  }
}
