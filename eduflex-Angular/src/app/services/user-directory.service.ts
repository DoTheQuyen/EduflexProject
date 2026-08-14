import { Injectable } from '@angular/core';
import { Observable, of, shareReplay } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { Client, UserFilterDto } from './api.services';

/**
 * Session-cached userId -> display name lookup, built off the existing (already
 * NSwag-regenerated) Users search endpoint. Exists because the Task module's DTOs only
 * ever carry AssignerUserId/AssigneeUserId — following the codebase's plain-ID
 * convention (see EnrolmentDto's EnquiryId/StudentApplicationId, which are the same
 * bare id-only shape) — so something has to resolve ids to names for the task list and
 * detail screens. One shared cache instead of every task component hand-rolling its own
 * searchUsers() call, same idea as staffOptions in enrolment-detail.component.ts but
 * not duplicated per component.
 */
@Injectable({ providedIn: 'root' })
export class UserDirectoryService {
  private namesById = new Map<string, string>();
  private loaded$?: Observable<void>;

  constructor(private apiClient: Client) {}

  private ensureLoaded(): Observable<void> {
    if (!this.loaded$) {
      this.loaded$ = this.apiClient
        .searchUsers(new UserFilterDto({ pageNumber: 1, pageSize: 500 }))
        .pipe(
          tap((result) => {
            for (const user of result.items ?? []) {
              if (user.id) {
                this.namesById.set(user.id, `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() || user.id);
              }
            }
          }),
          map(() => void 0),
          shareReplay(1)
        );
    }
    return this.loaded$;
  }

  // Synchronous best-effort lookup for templates — returns the raw id until the
  // directory has loaded, then the resolved name on the next change detection pass.
  getName(userId: string | undefined | null): string {
    if (!userId) return '';
    return this.namesById.get(userId) ?? userId;
  }

  load(): Observable<void> {
    return this.ensureLoaded();
  }
}
