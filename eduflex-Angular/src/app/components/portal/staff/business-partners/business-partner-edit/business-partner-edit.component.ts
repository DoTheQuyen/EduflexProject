import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Client, BusinessPartnerDto, BusinessPartnerContactDto, CreateBusinessPartnerDto } from '@services/api.services';
import { AuthHelperService, ModulePermissions } from '@services/auth-helper.service';
import { NotificationService } from '@services/notification.service';
import { FileUploaderComponent } from '@generic/file-uploader/file-uploader.component';
import { extractApiErrorMessage } from '../../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-business-partner-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, FileUploaderComponent],
  templateUrl: './business-partner-edit.component.html',
  styleUrls: ['./business-partner-edit.component.css']
})
export class BusinessPartnerEditComponent implements OnInit {
  partnerId: string | null = null;
  mode: 'view' | 'edit' = 'edit';
  permissions!: ModulePermissions;

  private partnerSnapshot: BusinessPartnerDto | null = null;

  isLoading = false;
  isSaving = false;
  isUploadingContract = false;
  errorMessage = '';
  contractFileNamePreview: string | null = null;
  private pendingContractFile: File | null = null;

  partnerForm: FormGroup;

  get contacts(): FormArray {
    return this.partnerForm.get('contacts') as FormArray;
  }

  constructor(
    private fb: FormBuilder,
    private apiClient: Client,
    private route: ActivatedRoute,
    private router: Router,
    private authHelper: AuthHelperService,
    private notificationService: NotificationService
  ) {
    this.permissions = this.authHelper.hasBusinessPartnersPermission();

    this.partnerForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      trademark: ['', [Validators.maxLength(100)]],
      address: ['', [Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
      phoneNumber: ['', [Validators.maxLength(30)]],
      abn: ['', [Validators.pattern(/^\d{11}$/)]],
      acn: ['', [Validators.pattern(/^\d{9}$/)]],
      commissionBaseRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      contractStartDate: [null],
      contractEndDate: [null],
      contractFileUrl: [''],
      contractFileName: [''],
      contacts: this.fb.array([])
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.partnerId = id;
    this.mode = id ? 'view' : 'edit';

    if (id) {
      this.loadPartner(id);
    } else {
      this.applyFormMode();
    }
  }

  loadPartner(id: string): void {
    this.isLoading = true;
    this.apiClient.businessPartnersGET(id).subscribe({
      next: (partner) => {
        this.partnerSnapshot = partner;
        this.applyPartnerToForm(partner);
        this.applyFormMode();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Could not load this business partner.');
      }
    });
  }

  private applyPartnerToForm(partner: BusinessPartnerDto): void {
    this.partnerForm.patchValue({
      name: partner.name,
      trademark: partner.trademark ?? '',
      address: partner.address ?? '',
      email: partner.email,
      phoneNumber: partner.phoneNumber,
      abn: partner.abn ?? '',
      acn: partner.acn ?? '',
      commissionBaseRate: partner.commissionBaseRate,
      contractStartDate: partner.contractStartDate ? this.toDateInputValue(partner.contractStartDate) : null,
      contractEndDate: partner.contractEndDate ? this.toDateInputValue(partner.contractEndDate) : null,
      contractFileUrl: partner.contractFileUrl ?? '',
      contractFileName: partner.contractFileName ?? ''
    });
    this.contractFileNamePreview = partner.contractFileName ?? null;

    this.contacts.clear();
    (partner.contacts ?? []).forEach(contact => this.contacts.push(this.buildContactGroup({
      firstName: contact.firstName ?? '',
      lastName: contact.lastName ?? '',
      email: contact.email ?? '',
      contactNo: contact.contactNo ?? ''
    })));
  }

  private toDateInputValue(value: unknown): string {
    const date = new Date(value as string);
    return date.toISOString().substring(0, 10);
  }

  private applyFormMode(): void {
    if (this.mode === 'view') {
      this.partnerForm.disable({ emitEvent: false });
    } else {
      this.partnerForm.enable({ emitEvent: false });
    }
  }

  enterEditMode(): void {
    if (!this.permissions.edit) {
      this.notificationService.error('You do not have permission to edit business partners.');
      return;
    }
    this.mode = 'edit';
    this.errorMessage = '';
    this.applyFormMode();
  }

  cancelEdit(): void {
    this.mode = 'view';
    this.errorMessage = '';
    this.pendingContractFile = null;
    if (this.partnerSnapshot) {
      this.applyPartnerToForm(this.partnerSnapshot);
    }
    this.applyFormMode();
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.partnerForm.get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  private buildContactGroup(value?: { firstName: string; lastName: string; email: string; contactNo: string }): FormGroup {
    return this.fb.group({
      firstName: [value?.firstName ?? '', [Validators.required]],
      lastName: [value?.lastName ?? '', [Validators.required]],
      email: [value?.email ?? '', [Validators.required, Validators.email]],
      contactNo: [value?.contactNo ?? '']
    });
  }

  addContact(): void {
    this.contacts.push(this.buildContactGroup());
  }

  removeContact(index: number): void {
    this.contacts.removeAt(index);
  }

  onContractFileSelected(file: File): void {
    this.errorMessage = '';
    this.pendingContractFile = file;
    this.contractFileNamePreview = file.name;
  }

  onSubmitPartner(): void {
    const requiredPermission = this.partnerId ? this.permissions.edit : this.permissions.add;
    if (!requiredPermission) {
      this.notificationService.error(this.partnerId ? 'You do not have permission to edit business partners.' : 'You do not have permission to add business partners.');
      return;
    }

    this.partnerForm.markAllAsTouched();
    this.errorMessage = '';

    if (this.partnerForm.invalid) {
      this.errorMessage = 'Please fix the highlighted fields before saving.';
      return;
    }

    this.isSaving = true;

    if (this.pendingContractFile) {
      this.isUploadingContract = true;
      this.apiClient.upload({ data: this.pendingContractFile, fileName: this.pendingContractFile.name }).subscribe({
        next: (result) => {
          this.isUploadingContract = false;
          this.partnerForm.patchValue({ contractFileUrl: result.url, contractFileName: this.pendingContractFile!.name });
          this.pendingContractFile = null;
          this.saveBusinessPartner();
        },
        error: (err) => {
          this.isUploadingContract = false;
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Could not upload the contract file. Please try again.');
        }
      });
    } else {
      this.saveBusinessPartner();
    }
  }

  private saveBusinessPartner(): void {
    // NSwag-generated DTOs call .toISOString() on date fields, so they need actual Date
    // objects — the <input type="date"> controls give plain "yyyy-MM-dd" strings.
    const formValue = this.partnerForm.value;
    const payload = new CreateBusinessPartnerDto({
      ...formValue,
      contractStartDate: formValue.contractStartDate ? new Date(formValue.contractStartDate) : undefined,
      contractEndDate: formValue.contractEndDate ? new Date(formValue.contractEndDate) : undefined,
      // NSwag's generated toJSON() calls .toJSON() on each array item, so nested DTOs
      // need to be actual class instances — plain objects from the FormArray don't have it.
      contacts: (formValue.contacts ?? []).map((c: any) => new BusinessPartnerContactDto(c))
    });

    if (this.partnerId) {
      this.apiClient.businessPartnersPUT(this.partnerId, payload).subscribe({
        next: () => {
          this.isSaving = false;
          this.mode = 'view';
          this.notificationService.success('Business partner updated successfully.');
          this.loadPartner(this.partnerId!);
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the business partner. Please try again.');
        }
      });
    } else {
      this.apiClient.businessPartnersPOST(payload).subscribe({
        next: (result) => {
          this.isSaving = false;
          this.notificationService.success('Business partner created successfully.');
          this.router.navigate(['/staff-portal/business-partners', result.id, 'edit'], { replaceUrl: true });
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = extractApiErrorMessage(err, 'Something went wrong saving the business partner. Please try again.');
        }
      });
    }
  }
}
