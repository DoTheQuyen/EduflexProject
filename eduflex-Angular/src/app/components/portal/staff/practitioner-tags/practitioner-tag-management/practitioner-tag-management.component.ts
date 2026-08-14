import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PractitionerTagService } from '@services/practitioner-tag.service';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { PractitionerTag, PRACTITIONER_TAG_COLOR_PRESETS } from '@app/models/practitioner-tag';
import { extractHttpErrorMessage } from '@app/shared/utils/http-error.util';

interface EditableTag {
  id: string | null;
  name: string;
  colorHex: string;
  description: string;
  active: boolean;
}

function blankTag(): EditableTag {
  return { id: null, name: '', colorHex: PRACTITIONER_TAG_COLOR_PRESETS[0], description: '', active: true };
}

@Component({
  selector: 'app-practitioner-tag-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './practitioner-tag-management.component.html',
  styleUrls: ['./practitioner-tag-management.component.css'],
})
export class PractitionerTagManagementComponent implements OnInit {
  readonly colorPresets = PRACTITIONER_TAG_COLOR_PRESETS;

  tags: PractitionerTag[] = [];
  isLoading = false;
  isSaving = false;
  permissions!: ModulePermissions;

  selectedTagId: string | null = null;
  form: EditableTag = blankTag();

  constructor(
    private tagService: PractitionerTagService,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService,
  ) {
    this.permissions = this.authHelper.hasVisaProcessTemplatesPermission();
  }

  ngOnInit(): void {
    this.loadTags();
  }

  loadTags(): void {
    this.isLoading = true;
    this.tagService.getAll().subscribe({
      next: (tags) => {
        this.tags = tags;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  selectTag(tag: PractitionerTag): void {
    this.selectedTagId = tag.id;
    this.form = { id: tag.id, name: tag.name, colorHex: tag.colorHex, description: tag.description ?? '', active: tag.active };
  }

  startNew(): void {
    this.selectedTagId = null;
    this.form = blankTag();
  }

  get isValid(): boolean {
    return this.form.name.trim().length > 0 && /^#[0-9a-fA-F]{6}$/.test(this.form.colorHex);
  }

  save(): void {
    if (!this.isValid) {
      this.notificationService.error('Give the tag a name and a valid hex colour.');
      return;
    }

    const payload = {
      name: this.form.name.trim(),
      colorHex: this.form.colorHex,
      description: this.form.description.trim() || undefined,
      active: this.form.active,
    };

    this.isSaving = true;

    if (this.form.id) {
      this.tagService.update(this.form.id, payload).subscribe({
        next: () => this.onSaveSuccess('Tag updated.'),
        error: (err) => this.onSaveError(err),
      });
    } else {
      this.tagService.create(payload).subscribe({
        next: (created) => {
          this.selectedTagId = created.id;
          this.onSaveSuccess('Tag created.');
        },
        error: (err) => this.onSaveError(err),
      });
    }
  }

  toggleActive(tag: PractitionerTag, event: Event): void {
    event.stopPropagation();
    this.tagService.setActive(tag.id, !tag.active).subscribe({
      next: () => {
        this.notificationService.success(tag.active ? 'Tag deactivated.' : 'Tag reactivated.');
        this.loadTags();
        if (this.selectedTagId === tag.id) {
          this.form.active = !tag.active;
        }
      },
      error: (err) => {
        this.notificationService.error(extractHttpErrorMessage(err, "Could not update this tag's status."));
      },
    });
  }

  private onSaveSuccess(message: string): void {
    this.isSaving = false;
    this.notificationService.success(message);
    this.loadTags();
  }

  private onSaveError(err: unknown): void {
    this.isSaving = false;
    this.notificationService.error(extractHttpErrorMessage(err, 'Could not save this tag.'));
  }
}
