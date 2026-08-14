import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { AccountSummary, AccountTimeline, ActionQueueResult, AccountType, AccountStatus } from '../models/accounts';
import { PagedResult } from '../models/enrolment';

// Hand-written against AccountsController's REST routes rather than the NSwag-generated
// Client — AccountsController isn't showing up in the generated client yet (its
// operations are missing from api.services.ts even though the backend compiles clean),
// so this talks to the routes directly the same way FinancialRecordService and
// InvoiceCustomSendComponent already do for routes NSwag hasn't caught up with. Migrate
// to the generated Client once that's sorted out.
@Injectable({ providedIn: 'root' })
export class AccountsService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Accounts`;

  constructor(private http: HttpClient) {}

  getActionQueue(windowDays = 14): Observable<ActionQueueResult> {
    const params = new HttpParams().set('windowDays', windowDays);
    return this.http.get<ActionQueueResult>(`${this.baseUrl}/action-queue`, { params });
  }

  getAccounts(options: {
    search?: string; accountType?: AccountType; status?: AccountStatus; pageNumber?: number; pageSize?: number;
  }): Observable<PagedResult<AccountSummary>> {
    let params = new HttpParams()
      .set('pageNumber', options.pageNumber ?? 1)
      .set('pageSize', options.pageSize ?? 20);
    if (options.search) params = params.set('search', options.search);
    if (options.accountType) params = params.set('accountType', options.accountType);
    if (options.status) params = params.set('status', options.status);

    return this.http.get<PagedResult<AccountSummary>>(this.baseUrl, { params });
  }

  getTimeline(accountType: AccountType, accountKey: string): Observable<AccountTimeline> {
    const params = new HttpParams().set('accountType', accountType).set('accountKey', accountKey);
    return this.http.get<AccountTimeline>(`${this.baseUrl}/timeline`, { params });
  }
}
