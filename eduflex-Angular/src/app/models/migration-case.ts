import { StepFieldDefinition, StepPrecondition, ProcessStepHint } from './visa-process-template';
import { Address, EmergencyContact } from './enrolment';
import { EnrolmentFormResponse } from './dynamic-form';

// Mirrors Eduflex/DTOs/MigrationCase/MigrationCaseDto.cs — the generic, category-agnostic
// case entity that runs on a VisaProcessTemplate snapshot. See
// docs/09-visa-process-config-module-design.md and MigrationCaseModel.cs for why this is
// independent of the Enrolment/VISA-tab system.

export type MigrationCaseStatus = 'Active' | 'Completed' | 'Withdrawn';
export type MigrationCaseStepStatus = 'Locked' | 'Draft' | 'Complete';

export interface MigrationCaseStep {
  key: string;
  order: number;
  label: string;
  description?: string | null;
  phase?: string | null;
  canReopen: boolean;
  practitionerTagId?: string | null;
  fieldsSnapshot: StepFieldDefinition[];
  requiredEvidenceCategoriesSnapshot: string[];
  preconditionsSnapshot: StepPrecondition[];
  hintsSnapshot: ProcessStepHint[];
  status: MigrationCaseStepStatus;
  fields: Record<string, string>;
  completedAt?: string | null;
  completedByName?: string | null;
}

export interface MigrationCaseDocument {
  id: string;
  fileName: string;
  category?: string | null;
  note?: string | null;
  url: string;
  contentType?: string | null;
  sizeBytes: number;
  uploadedByName: string;
  uploadedAt: string;
}

export interface MigrationCaseAuditEntry {
  id: string;
  description: string;
  performedByName: string;
  performedAt: string;
}

// Mirrors EnrolmentCommunication (models/enrolment.ts) — same underlying
// EnrolmentCommunicationModel shape on the backend, reused directly (see
// MigrationCaseDto.Communications). recipientType is a plain string rather than
// Enrolment's fixed RecipientType union, since a generic case has no equivalent fixed
// recipient roles.
export interface MigrationCaseCommunication {
  id: string;
  templateKey?: string | null;
  toEmail: string;
  recipientType: string;
  subject: string;
  body: string;
  attachedDocumentIds: string[];
  sentByName: string;
  sentAt: string;
}

export interface MigrationCase {
  id: string;
  ownerUserId: string;
  caseReference: string;
  templateId: string;
  templateVersion: number;
  templateName: string;
  country: string;
  category: string;
  primaryContactName: string;
  primaryContactEmail?: string | null;
  primaryContactMobile?: string | null;
  middleName?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  nationality?: string | null;
  passportNumber?: string | null;
  hometownAddress?: Address | null;
  currentAddress?: Address | null;
  emergencyContact?: EmergencyContact | null;
  notes?: string | null;
  status: MigrationCaseStatus;
  steps: MigrationCaseStep[];
  documents: MigrationCaseDocument[];
  communications: MigrationCaseCommunication[];
  // Reuses EnrolmentFormResponse directly (models/dynamic-form.ts) — same underlying
  // shape on the backend (MigrationCaseModel.FormResponses reuses
  // EnrolmentFormResponseModel), nothing Enrolment-specific in it.
  formResponses: EnrolmentFormResponse[];
  auditTrail: MigrationCaseAuditEntry[];
  createdAt: string;
}

export interface CreateMigrationCaseRequest {
  templateId: string;
  primaryContactName: string;
  primaryContactEmail?: string;
  primaryContactMobile?: string;
  notes?: string;
}

export interface UpdateCustomerInfoRequest {
  primaryContactName: string;
  primaryContactEmail?: string;
  primaryContactMobile?: string;
  middleName?: string;
  dateOfBirth?: string;
  gender?: string;
  nationality?: string;
  passportNumber?: string;
  hometownAddress?: Address;
  currentAddress?: Address;
  emergencyContact?: EmergencyContact;
}

export interface MigrationCaseFilterRequest {
  statuses?: MigrationCaseStatus[];
  ownerUserId?: string;
  category?: string;
  searchTerm?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export function caseStatusBadgeClass(status: MigrationCaseStatus): string {
  switch (status) {
    case 'Completed': return 'badge-pill-success-soft';
    case 'Withdrawn': return 'badge-pill-muted-soft';
    default: return 'badge-pill-accent-soft';
  }
}

export function stepStatusBadgeClass(status: MigrationCaseStepStatus): string {
  switch (status) {
    case 'Complete': return 'badge-pill-success-soft';
    case 'Draft': return 'badge-pill-accent-soft';
    default: return 'badge-pill-muted-soft';
  }
}
