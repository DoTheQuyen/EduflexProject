// Mirrors ShareService.Models.Accounts.AccountModels / Eduflex.DTOs.Accounts on the
// backend. "Account" granularity: one Student account = one Enrolment; one Partner
// account = one FinancialRecord (a partner with several enrolments shows several
// accounts, not one rolled-up total — see AccountsService's own comment on why).

export type AccountType = 'Student' | 'BusinessPartner' | 'EducationPartner';
export type AccountStatus = 'OnTrack' | 'AtRisk' | 'Overdue' | 'Complete';
export type ActionQueueReason = 'NotInvoiced' | 'Overdue' | 'Failed';
// 'Commission' only appears on Partner account entries — Student entries use the rest.
export type FeeType = 'Tuition' | 'ServiceFee' | 'VisaExtension' | 'Visa485' | 'PartnerVisa' | 'Other' | 'Commission';

export interface AccountSummary {
  accountType: AccountType;
  accountKey: string;
  enrolmentId: string;
  name: string;
  subLabel?: string;
  contractTotal: number;
  received: number;
  outstanding: number;
  nextDueDate?: string;
  openCount: number;
  status: AccountStatus;
}

export interface ActionQueueItem {
  accountType: AccountType;
  accountKey: string;
  enrolmentId: string;
  name: string;
  subLabel?: string;
  reason: ActionQueueReason;
  days: number;
  amount: number;
  scheduleLabel: string;
  entryId: string;
  linkedInvoiceId?: string;
}

export interface ActionQueueResult {
  items: ActionQueueItem[];
  totalAccounts: number;
  overdueAmount: number;
  overdueCount: number;
  dueToInvoiceAmount: number;
  dueToInvoiceCount: number;
}

export interface AccountTimelineEntry {
  entryId: string;
  feeType: FeeType;
  label: string;
  dueDate: string;
  amount: number;
  scheduleStatus: 'Planned' | 'Invoiced' | 'Skipped';
  skipReason?: string;
  linkedInvoiceId?: string;
  linkedInvoiceNo?: string;
  linkedInvoiceStatus?: 'Sent' | 'Paid' | 'Failed' | 'Cancelled';
  linkedInvoiceTotal?: number;
}

export interface AccountTimeline {
  accountType: AccountType;
  accountKey: string;
  enrolmentId: string;
  name: string;
  subLabel?: string;
  contractTotal: number;
  received: number;
  outstanding: number;
  nextDueDate?: string;
  entries: AccountTimelineEntry[];
}
