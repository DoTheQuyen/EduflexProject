import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client, SettingsDto, UpdateSettingsDto, DocumentUploadSettingsDto, UploadLimitDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { extractApiErrorMessage } from '../../../../shared/utils/api-error.util';

type SettingsTab = 'general' | 'documents' | 'images' | 'contracts' | 'enrolment' | 'chat';

@Component({
  selector: 'app-settings-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings-management.component.html',
  styleUrls: ['./settings-management.component.css']
})
export class SettingsManagementComponent implements OnInit {
  permissions!: ModulePermissions;
  isLoading = false;
  isSaving = false;
  errorMessage = '';

  activeTab: SettingsTab = 'general';
  readonly tabs: { key: SettingsTab; label: string }[] = [
    { key: 'general', label: 'General' },
    { key: 'documents', label: 'Application Documents' },
    { key: 'images', label: 'Images' },
    { key: 'contracts', label: 'Contracts' },
    { key: 'enrolment', label: 'Enrolment Documents' },
    { key: 'chat', label: 'Chat Assistant' }
  ];

  settingsForm: FormGroup;

  get defaultExtensions(): FormArray {
    return this.settingsForm.get(['documentUpload', 'default', 'allowedExtensions']) as FormArray;
  }

  get otherExtensions(): FormArray {
    return this.settingsForm.get(['documentUpload', 'other', 'allowedExtensions']) as FormArray;
  }

  get imageExtensions(): FormArray {
    return this.settingsForm.get(['imageUpload', 'allowedExtensions']) as FormArray;
  }

  get contractExtensions(): FormArray {
    return this.settingsForm.get(['contractUpload', 'allowedExtensions']) as FormArray;
  }

  get enrolmentExtensions(): FormArray {
    return this.settingsForm.get(['enrolmentUpload', 'allowedExtensions']) as FormArray;
  }

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasSettingsPermission();

    this.settingsForm = this.fb.group({
      feedbackDefaultLatestCount: [10, [Validators.required, Validators.min(1)]],
      coursePromotionDefaultLatestCount: [10, [Validators.required, Validators.min(1)]],
      maxApplicationsPerStudent: [1, [Validators.required, Validators.min(1)]],
      documentUpload: this.fb.group({
        default: this.fb.group({
          maxSizeMB: [5, [Validators.required, Validators.min(0.1)]],
          maxFileCount: [1, [Validators.required, Validators.min(1)]],
          allowedExtensions: this.fb.array([])
        }),
        other: this.fb.group({
          maxSizeMB: [5, [Validators.required, Validators.min(0.1)]],
          maxFileCount: [4, [Validators.required, Validators.min(1)]],
          allowedExtensions: this.fb.array([])
        })
      }),
      imageUpload: this.fb.group({
        maxSizeMB: [2, [Validators.required, Validators.min(0.1)]],
        maxFileCount: [1, [Validators.required, Validators.min(1)]],
        allowedExtensions: this.fb.array([])
      }),
      contractUpload: this.fb.group({
        maxSizeMB: [10, [Validators.required, Validators.min(0.1)]],
        maxFileCount: [1, [Validators.required, Validators.min(1)]],
        allowedExtensions: this.fb.array([])
      }),
      enrolmentUpload: this.fb.group({
        maxSizeMB: [10, [Validators.required, Validators.min(0.1)]],
        maxFileCount: [1, [Validators.required, Validators.min(1)]],
        allowedExtensions: this.fb.array([])
      }),
      chatSystemPrompt: ['', [Validators.required]],
      chatApiUrl: ['', [Validators.required]],
      chatGeminiModel: ['', [Validators.required]],
      chatGroqApiUrl: ['', [Validators.required]],
      chatGroqModel: ['', [Validators.required]],
      chatOpenRouterApiUrl: ['', [Validators.required]],
      chatOpenRouterModel: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    if (!this.permissions.view) {
      this.notificationService.error('You do not have permission to view settings.');
      return;
    }
    this.load();
  }

  private load(): void {
    this.isLoading = true;
    this.apiClient.settingsGET().subscribe({
      next: (settings: SettingsDto) => {
        this.applySettingsToForm(settings);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load settings.');
      }
    });
  }

  private applySettingsToForm(settings: SettingsDto): void {
    this.settingsForm.patchValue({
      feedbackDefaultLatestCount: settings.feedbackDefaultLatestCount,
      coursePromotionDefaultLatestCount: settings.coursePromotionDefaultLatestCount,
      maxApplicationsPerStudent: settings.maxApplicationsPerStudent,
      documentUpload: {
        default: {
          maxSizeMB: settings.documentUpload?.default?.maxSizeMB,
          maxFileCount: settings.documentUpload?.default?.maxFileCount
        },
        other: {
          maxSizeMB: settings.documentUpload?.other?.maxSizeMB,
          maxFileCount: settings.documentUpload?.other?.maxFileCount
        }
      },
      imageUpload: {
        maxSizeMB: settings.imageUpload?.maxSizeMB,
        maxFileCount: settings.imageUpload?.maxFileCount
      },
      contractUpload: {
        maxSizeMB: settings.contractUpload?.maxSizeMB,
        maxFileCount: settings.contractUpload?.maxFileCount
      },
      enrolmentUpload: {
        maxSizeMB: settings.enrolmentUpload?.maxSizeMB,
        maxFileCount: settings.enrolmentUpload?.maxFileCount
      },
      chatSystemPrompt: settings.chatSystemPrompt,
      chatApiUrl: settings.chatApiUrl,
      chatGeminiModel: settings.chatGeminiModel,
      chatGroqApiUrl: settings.chatGroqApiUrl,
      chatGroqModel: settings.chatGroqModel,
      chatOpenRouterApiUrl: settings.chatOpenRouterApiUrl,
      chatOpenRouterModel: settings.chatOpenRouterModel
    });

    this.setExtensions(this.defaultExtensions, settings.documentUpload?.default?.allowedExtensions ?? []);
    this.setExtensions(this.otherExtensions, settings.documentUpload?.other?.allowedExtensions ?? []);
    this.setExtensions(this.imageExtensions, settings.imageUpload?.allowedExtensions ?? []);
    this.setExtensions(this.contractExtensions, settings.contractUpload?.allowedExtensions ?? []);
    this.setExtensions(this.enrolmentExtensions, settings.enrolmentUpload?.allowedExtensions ?? []);

    if (this.permissions.edit) {
      this.settingsForm.enable({ emitEvent: false });
    } else {
      this.settingsForm.disable({ emitEvent: false });
    }
  }

  private setExtensions(array: FormArray, values: string[]): void {
    array.clear();
    values.forEach(value => array.push(this.fb.control(value)));
  }

  isFieldInvalid(path: string | string[]): boolean {
    const control = this.settingsForm.get(path);
    return control ? control.invalid && control.touched : false;
  }

  addExtension(array: FormArray, input: HTMLInputElement): void {
    let value = input.value.trim().toLowerCase();
    if (!value) {
      return;
    }
    if (!value.startsWith('.')) {
      value = '.' + value;
    }
    if (!array.value.includes(value)) {
      array.push(this.fb.control(value));
    }
    input.value = '';
  }

  removeExtension(array: FormArray, index: number): void {
    array.removeAt(index);
  }

  private controlsForTab(tab: SettingsTab): AbstractControl[] {
    switch (tab) {
      case 'general':
        return [this.settingsForm.get('feedbackDefaultLatestCount')!, this.settingsForm.get('coursePromotionDefaultLatestCount')!, this.settingsForm.get('maxApplicationsPerStudent')!];
      case 'documents':
        return [this.settingsForm.get(['documentUpload', 'default'])!, this.settingsForm.get(['documentUpload', 'other'])!];
      case 'images':
        return [this.settingsForm.get('imageUpload')!];
      case 'contracts':
        return [this.settingsForm.get('contractUpload')!];
      case 'enrolment':
        return [this.settingsForm.get('enrolmentUpload')!];
      case 'chat':
        return [
          this.settingsForm.get('chatSystemPrompt')!,
          this.settingsForm.get('chatApiUrl')!,
          this.settingsForm.get('chatGeminiModel')!,
          this.settingsForm.get('chatGroqApiUrl')!,
          this.settingsForm.get('chatGroqModel')!,
          this.settingsForm.get('chatOpenRouterApiUrl')!,
          this.settingsForm.get('chatOpenRouterModel')!
        ];
    }
  }

  private extensionsErrorForTab(tab: SettingsTab): string | null {
    switch (tab) {
      case 'documents':
        return this.defaultExtensions.length === 0 || this.otherExtensions.length === 0
          ? 'Add at least one allowed file extension for both Default and Other.'
          : null;
      case 'images':
        return this.imageExtensions.length === 0 ? 'Add at least one allowed file extension.' : null;
      case 'contracts':
        return this.contractExtensions.length === 0 ? 'Add at least one allowed file extension.' : null;
      default:
        return null;
    }
  }

  saveSection(tab: SettingsTab): void {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to update settings.');
      return;
    }

    const controls = this.controlsForTab(tab);
    controls.forEach(c => c.markAllAsTouched());
    this.errorMessage = '';

    if (controls.some(c => c.invalid)) {
      return;
    }
    const extensionError = this.extensionsErrorForTab(tab);
    if (extensionError) {
      this.errorMessage = extensionError;
      return;
    }

    this.isSaving = true;
    const value = this.settingsForm.value;
    const payload = new UpdateSettingsDto({
      feedbackDefaultLatestCount: value.feedbackDefaultLatestCount,
      coursePromotionDefaultLatestCount: value.coursePromotionDefaultLatestCount,
      maxApplicationsPerStudent: value.maxApplicationsPerStudent,
      documentUpload: new DocumentUploadSettingsDto({
        default: new UploadLimitDto(value.documentUpload.default),
        other: new UploadLimitDto(value.documentUpload.other)
      }),
      imageUpload: new UploadLimitDto(value.imageUpload),
      contractUpload: new UploadLimitDto(value.contractUpload),
      enrolmentUpload: new UploadLimitDto(value.enrolmentUpload),
      chatSystemPrompt: value.chatSystemPrompt,
      chatApiUrl: value.chatApiUrl,
      chatGeminiModel: value.chatGeminiModel,
      chatGroqApiUrl: value.chatGroqApiUrl,
      chatGroqModel: value.chatGroqModel,
      chatOpenRouterApiUrl: value.chatOpenRouterApiUrl,
      chatOpenRouterModel: value.chatOpenRouterModel
    });

    this.apiClient.settingsPUT(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.notificationService.success('Settings updated successfully.');
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving settings. Please try again.');
      }
    });
  }
}