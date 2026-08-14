import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Client, EnrolmentFilterDto, EnquiryFilterDto, FinancialRecordFilterDto } from '@services/api.services';
import { MigrationCaseService } from '@services/migration-case.service';
import { DataTableColumn } from '@generic/data-table/data-table.models';
import { RecordPickerPage } from '@generic/record-picker/record-picker.component';
import { LinkedRecordType } from '../../../../models/task';

export interface LinkPickerConfig {
  title: string;
  columns: DataTableColumn<any>[];
  searchFn: (searchTerm: string, pageNumber: number, pageSize: number) => Observable<RecordPickerPage<any>>;
  label: (row: any) => string;
}

// Shared between task-new and task-detail (both need the same four "link a record"
// pickers) so the column/search-function wiring for Enrolment/Enquiry/Application/
// FinancialRecord lives in exactly one place. Not a component itself — just config
// handed to the generic app-record-picker, which knows nothing about these four types.
export function buildLinkPickerConfigs(apiClient: Client, migrationCaseService: MigrationCaseService): Record<LinkedRecordType, LinkPickerConfig> {
  const fullName = (row: any) => `${row.firstName ?? ''} ${row.lastName ?? ''}`.trim();

  return {
    Enrolment: {
      title: 'Select an Enrolment',
      columns: [
        { field: 'firstName', title: 'Student', formatter: (_v: unknown, row: any) => fullName(row) },
        { field: 'email', title: 'Email' },
        { field: 'status', title: 'Status' },
        { field: 'actions', title: '', minWidth: '90px' }
      ],
      searchFn: (searchTerm, pageNumber, pageSize) =>
        apiClient.searchEnrolments(new EnrolmentFilterDto({ pageNumber, pageSize, searchTerm: searchTerm || undefined })).pipe(
          map((r) => ({ items: r.items ?? [], totalCount: r.totalCount ?? 0, pageNumber: r.pageNumber ?? pageNumber, pageSize: r.pageSize ?? pageSize }))
        ),
      label: (row: any) => fullName(row) || row.id
    },
    Enquiry: {
      title: 'Select an Enquiry',
      columns: [
        { field: 'firstName', title: 'Name', formatter: (_v: unknown, row: any) => fullName(row) },
        { field: 'email', title: 'Email' },
        { field: 'status', title: 'Status' },
        { field: 'actions', title: '', minWidth: '90px' }
      ],
      searchFn: (searchTerm, pageNumber, pageSize) =>
        apiClient.searchEnquiries(new EnquiryFilterDto({ pageNumber, pageSize, searchTerm: searchTerm || undefined })).pipe(
          map((r) => ({ items: r.items ?? [], totalCount: r.totalCount ?? 0, pageNumber: r.pageNumber ?? pageNumber, pageSize: r.pageSize ?? pageSize }))
        ),
      label: (row: any) => fullName(row) || row.id
    },
    Application: {
      title: 'Select an Application',
      columns: [
        { field: 'studentName', title: 'Student' },
        { field: 'applicationType', title: 'Type' },
        { field: 'status', title: 'Status' },
        { field: 'actions', title: '', minWidth: '90px' }
      ],
      // applicationsGET takes positional params, not a filter DTO — different shape
      // from the other three (see ApplicationsController; this endpoint predates the
      // filter-object convention the newer modules use).
      searchFn: (searchTerm, pageNumber, pageSize) =>
        apiClient.applicationsGET(pageNumber, pageSize, searchTerm || undefined).pipe(
          map((r) => ({ items: r.items ?? [], totalCount: r.totalCount ?? 0, pageNumber: r.pageNumber ?? pageNumber, pageSize: r.pageSize ?? pageSize }))
        ),
      label: (row: any) => row.studentName || row.id
    },
    FinancialRecord: {
      title: 'Select a Financial Record',
      columns: [
        { field: 'studentName', title: 'Student' },
        { field: 'enrolmentStatus', title: 'Enrolment Status' },
        { field: 'totalTuition', title: 'Tuition' },
        { field: 'actions', title: '', minWidth: '90px' }
      ],
      searchFn: (searchTerm, pageNumber, pageSize) =>
        apiClient.searchFinancialRecords(new FinancialRecordFilterDto({ pageNumber, pageSize, searchTerm: searchTerm || undefined })).pipe(
          map((r) => ({ items: r.items ?? [], totalCount: r.totalCount ?? 0, pageNumber: r.pageNumber ?? pageNumber, pageSize: r.pageSize ?? pageSize }))
        ),
      label: (row: any) => row.studentName || row.id
    },
    // MigrationCasesController is new enough that NSwag hasn't been regenerated against
    // it yet — hand-written MigrationCaseService, same bypass-NSwag reasoning as the rest
    // of the Migration Case module, not apiClient like the other four link types here.
    MigrationCase: {
      title: 'Select a Migration Case',
      columns: [
        { field: 'caseReference', title: 'Case #' },
        { field: 'primaryContactName', title: 'Contact' },
        { field: 'category', title: 'Category' },
        { field: 'status', title: 'Status' },
        { field: 'actions', title: '', minWidth: '90px' }
      ],
      searchFn: (searchTerm, pageNumber, pageSize) =>
        migrationCaseService.search({ pageNumber, pageSize, searchTerm: searchTerm || undefined }).pipe(
          map((r) => ({ items: r.items ?? [], totalCount: r.totalCount ?? 0, pageNumber: r.pageNumber ?? pageNumber, pageSize: r.pageSize ?? pageSize }))
        ),
      label: (row: any) => `${row.caseReference} — ${row.primaryContactName}` || row.id
    }
  };
}

// Resolves a single already-linked record's display label — used when a task detail
// page loads with e.g. EnrolmentId already set and needs to show something better than
// a raw ObjectId string.
export function resolveLinkedRecordLabel(
  apiClient: Client, type: LinkedRecordType, id: string, migrationCaseService?: MigrationCaseService
): Observable<string> {
  switch (type) {
    case 'Enrolment':
      return apiClient.enrolmentsGET(id).pipe(map((r) => `${r.firstName ?? ''} ${r.lastName ?? ''}`.trim() || id));
    case 'Enquiry':
      return apiClient.enquiriesGET(id).pipe(map((r) => `${r.firstName ?? ''} ${r.lastName ?? ''}`.trim() || id));
    case 'Application':
      return apiClient.applicationsGET2(id).pipe(map((r) => r.studentName || id));
    case 'FinancialRecord':
      return apiClient.financialRecords(id).pipe(map((r) => r.studentName || id));
    case 'MigrationCase':
      if (!migrationCaseService) {
        return new Observable<string>((subscriber) => { subscriber.next(id); subscriber.complete(); });
      }
      return migrationCaseService.getById(id).pipe(map((c) => `${c.caseReference} — ${c.primaryContactName}` || id));
  }
}
