// Mirrors Eduflex/DTOs/VisaProcess/VisaProcessTemplateDto.cs. This module (the Process
// Designer) is NOT yet wired into the live VISA Process tab or EnrolmentService — see
// docs/09-visa-process-config-module-design.md. It's a standalone content-authoring
// screen today, same relationship DynamicFormTemplate has to the rest of the app before
// a form is actually bound/requested.

export type TemplateStatus = 'Active' | 'Inactive';
export type StepFieldInputType = 'Text' | 'Date' | 'Number' | 'Select' | 'YesNo';
export type StepPreconditionType =
  | 'PriorStepFieldNotEmpty'
  | 'CourseApplicationFinalized'
  | 'FieldValueIn'
  | 'AllPriorEvidenceUploaded';

export const STEP_FIELD_INPUT_TYPES: StepFieldInputType[] = ['Text', 'Date', 'Number', 'Select', 'YesNo'];
export const STEP_PRECONDITION_TYPES: StepPreconditionType[] = [
  'PriorStepFieldNotEmpty',
  'CourseApplicationFinalized',
  'FieldValueIn',
  'AllPriorEvidenceUploaded'
];

export interface StepFieldDefinition {
  fieldKey: string;
  label: string;
  inputType: StepFieldInputType;
  options: string[];
  isRequired: boolean;
}

export interface StepPrecondition {
  type: StepPreconditionType;
  sourceStepKey?: string | null;
  fieldKey?: string | null;
  allowedValues: string[];
  detail?: string | null;
}

export interface ProcessStepHint {
  id: string;
  text: string;
  authorUserId?: string | null;
  authorName?: string | null;
  createdAt: string;
  pinned: boolean;
}

export interface VisaProcessStepDefinition {
  key: string;
  order: number;
  label: string;
  description?: string | null;
  phase?: string | null;
  enabled: boolean;
  canReopen: boolean;
  practitionerTagId?: string | null;
  fields: StepFieldDefinition[];
  requiredEvidenceCategories: string[];
  preconditions: StepPrecondition[];
  setsEnrolmentStatusTo?: string | null;
  hints: ProcessStepHint[];
}

export interface VisaProcessTemplate {
  id: string;
  name: string;
  country: string;
  category: string;
  description?: string | null;
  status: TemplateStatus;
  isDefaultForCountry: boolean;
  version: number;
  steps: VisaProcessStepDefinition[];
}

export interface SaveVisaProcessTemplateRequest {
  name: string;
  country: string;
  category: string;
  description?: string | null;
  status: TemplateStatus;
  isDefaultForCountry: boolean;
  steps: VisaProcessStepDefinition[];
}

export function templateStatusBadgeClass(status: TemplateStatus): string {
  return status === 'Active' ? 'badge-pill-success-soft' : 'badge-pill-muted-soft';
}

export function newStepDefinition(order: number): VisaProcessStepDefinition {
  return {
    key: '',
    order,
    label: '',
    description: '',
    phase: 'Application',
    enabled: true,
    canReopen: true,
    practitionerTagId: null,
    fields: [],
    requiredEvidenceCategories: [],
    preconditions: [],
    setsEnrolmentStatusTo: null,
    hints: []
  };
}

export function newFieldDefinition(): StepFieldDefinition {
  return { fieldKey: '', label: '', inputType: 'Text', options: [], isRequired: false };
}

export function newPrecondition(): StepPrecondition {
  return { type: 'PriorStepFieldNotEmpty', sourceStepKey: '', fieldKey: '', allowedValues: [], detail: '' };
}
