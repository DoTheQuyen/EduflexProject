import { VisaStepKey } from './enrolment';

export type AnswerType = 'RichText' | 'YesNo' | 'SingleSelect' | 'MultiSelect';

// Shared with the backend's DynamicFormLimits.RichTextAnswerMaxLength (ShareService/
// Enums/DynamicForms/DynamicFormEnums.cs) — keep both in sync if this changes. The
// backend enforces this for real; this is just so the UI can show/limit it live.
export const RICH_TEXT_MAX_LENGTH = 2000;

export const ANSWER_TYPE_LABELS: Record<AnswerType, string> = {
  RichText: 'Rich text',
  YesNo: 'Yes / No',
  SingleSelect: 'Single select',
  MultiSelect: 'Multi select'
};

export type TemplateStatus = 'Active' | 'Inactive';
export type FormResponseStatus = 'Requesting' | 'Draft' | 'Responded' | 'Withdrawn' | 'Archived';

export const FORM_RESPONSE_STATUS_LABELS: Record<FormResponseStatus, string> = {
  Requesting: 'Requesting',
  Draft: 'Draft',
  Responded: 'Responded',
  Withdrawn: 'Withdrawn',
  Archived: 'Archived'
};

export interface FormQuestion {
  id: string;
  order: number;
  questionText: string;
  helpText?: string;
  answerType: AnswerType;
  options: string[];
  isRequired: boolean;
  // Only meaningful for RichText questions. Undefined = use RICH_TEXT_MAX_LENGTH.
  maxLength?: number;
  // Only meaningful for SingleSelect/MultiSelect questions. False/undefined = vertical.
  optionsHorizontal?: boolean;
}

export interface DynamicFormTemplate {
  id: string;
  name: string;
  description?: string;
  status: TemplateStatus;
  boundStepKey?: VisaStepKey;
  questions: FormQuestion[];
}

export interface SaveDynamicFormTemplateRequest {
  name: string;
  description?: string;
  status: TemplateStatus;
  boundStepKey?: VisaStepKey | null;
  questions: FormQuestion[];
}

export interface FormAnswer {
  questionId: string;
  textValue?: string;
  selectedOptions: string[];
}

export interface EnrolmentFormResponse {
  id: string;
  formTemplateId: string;
  formName: string;
  questionsSnapshot: FormQuestion[];
  boundStepKey?: VisaStepKey;
  status: FormResponseStatus;
  answers: FormAnswer[];
  requestedByName: string;
  requestedAt: string;
  lastSavedAt?: string;
  submittedAt?: string;
  withdrawnAt?: string;
  staffEditedAt?: string;
  exportedDocumentId?: string;
}

export function templateStatusBadgeClass(status: TemplateStatus): string {
  return status === 'Active' ? 'badge-pill-success-soft' : 'badge-pill-muted-soft';
}

export function formResponseStatusBadgeClass(status: FormResponseStatus): string {
  switch (status) {
    case 'Responded': return 'badge-pill-success-soft';
    case 'Draft': return 'badge-pill-accent-soft';
    case 'Requesting': return 'badge-pill-navy-soft';
    case 'Withdrawn': return 'badge-pill-muted-soft';
    case 'Archived': return 'badge-pill-muted-soft';
  }
}

export interface MyEnrolmentForms {
  enrolmentId: string;
  forms: EnrolmentFormResponse[];
}

export function newQuestion(order: number): FormQuestion {
  return {
    id: '',
    order,
    questionText: '',
    helpText: '',
    answerType: 'RichText',
    options: [],
    isRequired: false
  };
}

export function answerFor(answers: FormAnswer[], questionId: string): FormAnswer | undefined {
  return answers.find(a => a.questionId === questionId);
}
