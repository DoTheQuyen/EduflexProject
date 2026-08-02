import type { EnrolmentFormResponse } from './dynamic-form';
import { templateStatusBadgeClass } from './dynamic-form';

export interface Address {
  street?: string;
  suburb?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
}

export interface EmergencyContact {
  name?: string;
  relationship?: string;
  phone?: string;
  email?: string;
}

// Evidence categories used to tag uploads to a VISA Process step. 'Other' covers
// anything uploaded outside the step-gated flow (e.g. via the Documents tab directly).
export type DocumentCategory = 'GS' | 'UniOffer' | 'CoE' | 'VisaDraft' | 'VisaGranted' | 'Other';

export const DOCUMENT_CATEGORY_LABELS: Record<DocumentCategory, string> = {
  GS: 'GS Statement',
  UniOffer: 'Uni Offer',
  CoE: 'CoE',
  VisaDraft: 'VISA Draft',
  VisaGranted: 'VISA Granted',
  Other: 'Other'
};

// Shared by the Documents and Communication tabs so a document's category renders as
// its friendly label without either component needing its own copy of the lookup.
export function documentCategoryLabel(category?: string): string {
  if (!category) return 'Other';
  return (DOCUMENT_CATEGORY_LABELS as Record<string, string>)[category] ?? category;
}

export interface EnrolmentDocument {
  id: string;
  fileName: string;
  category?: DocumentCategory | string;
  url: string;
  contentType?: string;
  sizeBytes: number;
  uploadedByUserId?: string;
  uploadedByName: string;
  isFromStudent: boolean;
  uploadedAt: string;
}

export type RecipientType = 'Student' | 'EducationPartner' | 'BusinessPartner';

export interface EnrolmentCommunication {
  id: string;
  templateKey?: string;
  toEmail: string;
  recipientType: RecipientType;
  subject: string;
  body: string;
  attachedDocumentIds: string[];
  sentByName: string;
  sentAt: string;
}

export interface EnrolmentAuditEntry {
  id: string;
  description: string;
  performedByName: string;
  performedAt: string;
}

// Order matters — this IS the unlock sequence the VISA Process tab renders and gates on.
export type VisaStepKey = 'StudentInfo' | 'EnrolmentForm' | 'ApplyOffer' | 'CoeCompletion' | 'VisaApplication' | 'VisaOutcome';
export type VisaStepStatus = 'Locked' | 'Draft' | 'Complete';

export const VISA_STEP_ORDER: VisaStepKey[] = [
  'StudentInfo', 'EnrolmentForm', 'ApplyOffer', 'CoeCompletion', 'VisaApplication', 'VisaOutcome'
];

// Shared with the Dynamic Forms module (bound-step picker/labels), not just the
// VISA Process tab — kept here alongside VISA_STEP_ORDER as the one source of truth.
export const VISA_STEP_LABELS: Record<VisaStepKey, string> = {
  StudentInfo: 'Student Info',
  EnrolmentForm: 'Enrolment Form',
  ApplyOffer: 'Apply Offer',
  CoeCompletion: 'CoE Completion',
  VisaApplication: 'VISA Application',
  VisaOutcome: 'VISA Outcome'
};

// EnrolmentForm's 'GS' entry is here only so its upload zone renders — that step is
// already Complete by the time it's shown (auto-completed at creation), so unlike the
// other four it's never actually *required* to unlock anything.
export const VISA_STEP_EVIDENCE_CATEGORY: Partial<Record<VisaStepKey, DocumentCategory>> = {
  EnrolmentForm: 'GS',
  ApplyOffer: 'UniOffer',
  CoeCompletion: 'CoE',
  VisaApplication: 'VisaDraft',
  VisaOutcome: 'VisaGranted'
};

export interface VisaProcessStep {
  key: VisaStepKey;
  status: VisaStepStatus;
  fields: Record<string, string>;
  completedByName?: string;
  completedAt?: string;
}

export type EnrolmentStatus = 'Draft' | 'Offer' | 'Coe' | 'ApplyVisa' | 'VisaSuccess' | 'VisaFail' | 'Cancel';

export interface Enrolment {
  id: string;
  ownerUserId: string;
  ownerName: string;
  studentUserId: string;
  studentApplicationId?: string;
  enquiryId?: string;

  firstName: string;
  middleName?: string;
  lastName: string;
  dateOfBirth?: string;
  gender?: string;
  email: string;
  mobile: string;
  nationality?: string;
  passportNumber?: string;
  hometownAddress?: Address;
  currentAddress?: Address;
  emergencyContact?: EmergencyContact;

  educationPartnerId?: string;
  courseId?: string;
  intake?: string;
  studyMode?: string;
  campus?: string;
  commencementDate?: string;
  actualCommencementDate?: string;
  expectedCompletionDate?: string;
  fundingSource?: string;
  visaStatus?: string;
  status: EnrolmentStatus;
  notes?: string;
  tuitionFee?: number;
  // Server-owned, derived from the linked EducationPartner — hidden/read-only in the UI.
  businessPartnerId?: string;

  documents: EnrolmentDocument[];
  communications: EnrolmentCommunication[];
  formResponses: EnrolmentFormResponse[];
  auditTrail: EnrolmentAuditEntry[];
  visaProcessSteps: VisaProcessStep[];

  createdAt: string;
  updatedAt: string;
}

export interface CreateEnrolmentRequest {
  studentId?: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  dateOfBirth?: string;
  gender?: string;
  email: string;
  mobile: string;
  nationality?: string;
  passportNumber?: string;
  hometownAddress?: Address;
  currentAddress?: Address;
  emergencyContact?: EmergencyContact;

  educationPartnerId?: string;
  courseId?: string;
  intake?: string;
  studyMode?: string;
  campus?: string;
  commencementDate?: string;
  actualCommencementDate?: string;
  expectedCompletionDate?: string;
  fundingSource?: string;
  visaStatus?: string;
  notes?: string;
  tuitionFee?: number;
}

export interface EnrolmentFilter {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  statuses?: EnrolmentStatus[];
  mineOnly?: boolean;
}

export interface EnrolmentStatusOption {
  value: EnrolmentStatus;
  label: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface EmailTemplate {
  id: string;
  key: string;
  name: string;
  subject: string;
  body: string;
  isSystemDefault: boolean;
  isActive: boolean;
}

// Reuses the DynamicForms module's badge styling (identical Active/Inactive shape)
// rather than duplicating the color-class mapping — see templateStatusBadgeClass.
export function emailTemplateStatusBadgeClass(template: EmailTemplate): string {
  return templateStatusBadgeClass(template.isActive ? 'Active' : 'Inactive');
}
