import type { Address, EmergencyContact } from './enrolment';

export type ApplicationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Studying';

// 'Studying' means the application was finalized into an Enrolment (see
// EnrolmentModel.StudentApplicationId); 'Rejected' is the terminal decline.
// Neither counts as "still open" for the Student Details active-applications list.
export const ACTIVE_APPLICATION_STATUSES: ApplicationStatus[] = ['Pending', 'Approved'];

export interface Application {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  description: string;
  details: string;
  applicationType: string;
  dateApplied: string;
  status: ApplicationStatus;
  createdAt: string;
  updatedAt: string;
}

// Staff-facing read view of a single application — used by the "View application" link
// on the Student Details page and the Enrolment history panel's "From application" link.
export interface ApplicationDetail {
  id: string;
  studentId: string;
  studentName: string;
  description: string;
  dateApplied: string;
  status: ApplicationStatus;
  details: string;
  applicationType: string;
  studyMode?: string;
  campus?: string;
  hometownAddress?: Address;
  currentAddress?: Address;
  emergencyContact?: EmergencyContact;
}
