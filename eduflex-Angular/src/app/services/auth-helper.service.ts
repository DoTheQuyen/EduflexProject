import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { PermissionKey } from '@services/api.services';
import { Client } from '@services/public.services';

export interface ModulePermissions {
  view: boolean;
  add: boolean;
  edit: boolean;
  delete: boolean;
  reassign?: boolean;
}

// Tasks has no delete key (tasks are completed/reopened, never deleted) and adds
// viewAll — the Manager/Admin-only key for the department-scoped All Tasks page — so it
// doesn't fit the standard ModulePermissions shape.
export interface TaskPermissions {
  view: boolean;
  add: boolean;
  edit: boolean;
  viewAll: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AuthHelperService {
  constructor(
    @Inject(PLATFORM_ID) private platformId: Object,
    private authClient: Client,
  ) {}

  isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    return !!sessionStorage.getItem('userData');
  }

  getCurrentUser(): any {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      const userData = sessionStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  }

  getUserInfo(): any {
    return this.getCurrentUser();
  }

  // The real session lives in the httpOnly cookies now, so this also tells the
  // backend to revoke the refresh token — this only clears the local UI snapshot.
  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.authClient.logout().subscribe({ error: () => {} });
      sessionStorage.removeItem('userData');
      sessionStorage.removeItem('rememberMe');
      sessionStorage.removeItem('userEmail');
    }
  }

  getUserRole(): string {
    const userInfo = this.getCurrentUser();
    return userInfo?.role || 'student';
  }

  hasPermission(permissionKey: string): boolean {
    const userInfo = this.getCurrentUser();
    return !!userInfo?.permissions?.includes(permissionKey);
  }

  hasUsersPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.UsersView),
      add: this.hasPermission(PermissionKey.UsersAdd),
      edit: this.hasPermission(PermissionKey.UsersEdit),
      delete: this.hasPermission(PermissionKey.UsersDelete),
    };
  }

  hasCoursePromotionsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.CoursePromotionsView),
      add: this.hasPermission(PermissionKey.CoursePromotionsAdd),
      edit: this.hasPermission(PermissionKey.CoursePromotionsEdit),
      delete: this.hasPermission(PermissionKey.CoursePromotionsDelete),
    };
  }

  hasRolesPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.RolesView),
      add: this.hasPermission(PermissionKey.RolesAdd),
      edit: this.hasPermission(PermissionKey.RolesEdit),
      delete: this.hasPermission(PermissionKey.RolesDelete),
    };
  }

  hasEnquiryPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EnquiryView),
      add: this.hasPermission(PermissionKey.EnquiryAdd),
      edit: this.hasPermission(PermissionKey.EnquiryEdit),
      delete: this.hasPermission(PermissionKey.EnquiryDelete),
    };
  }

  hasEducationPartnersPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EducationPartnersView),
      add: this.hasPermission(PermissionKey.EducationPartnersAdd),
      edit: this.hasPermission(PermissionKey.EducationPartnersEdit),
      delete: this.hasPermission(PermissionKey.EducationPartnersDelete),
    };
  }

  hasBusinessPartnersPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.BusinessPartnersView),
      add: this.hasPermission(PermissionKey.BusinessPartnersAdd),
      edit: this.hasPermission(PermissionKey.BusinessPartnersEdit),
      delete: this.hasPermission(PermissionKey.BusinessPartnersDelete),
    };
  }

  hasEnrolmentsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EnrolmentsView),
      add: this.hasPermission(PermissionKey.EnrolmentsAdd),
      edit: this.hasPermission(PermissionKey.EnrolmentsEdit),
      delete: this.hasPermission(PermissionKey.EnrolmentsDelete),
      reassign: this.hasPermission(PermissionKey.EnrolmentsReassign),
    };
  }

  hasFinancePermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.FinanceView),
      add: this.hasPermission(PermissionKey.FinanceAdd),
      edit: this.hasPermission(PermissionKey.FinanceEdit),
      delete: this.hasPermission(PermissionKey.FinanceDelete),
    };
  }

  hasStudentsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.StudentsView),
      add: this.hasPermission(PermissionKey.StudentsAdd),
      edit: this.hasPermission(PermissionKey.StudentsEdit),
      delete: this.hasPermission(PermissionKey.StudentsDelete),
    };
  }

  hasSettingsPermission(): ModulePermissions {
    const canEdit = this.hasPermission(PermissionKey.SettingsEdit);
    return {
      view: canEdit,
      add: false,
      edit: canEdit,
      delete: false,
    };
  }

  // TODO(department-migration): switch these string literals to PermissionKey.Departments*
  // once `nswag run` has been re-run against the updated backend — the enum doesn't have
  // these members yet because api.services.ts hasn't been regenerated.
  hasDepartmentsPermission(): ModulePermissions {
    return {
      view: this.hasPermission('DepartmentsView'),
      add: this.hasPermission('DepartmentsAdd'),
      edit: this.hasPermission('DepartmentsEdit'),
      delete: this.hasPermission('DepartmentsDelete'),
    };
  }

  // TODO: switch to PermissionKey.DynamicFormsEdit once `nswag run` has been re-run
  // against the updated backend. Single flat key, same shape as hasSettingsPermission —
  // Dynamic Forms template management is admin-only, no separate Add/Delete keys.
  hasDynamicFormsPermission(): ModulePermissions {
    const canEdit = this.hasPermission('DynamicFormsEdit');
    return {
      view: canEdit,
      add: canEdit,
      edit: canEdit,
      delete: canEdit,
    };
  }

  // TODO: switch these string literals to PermissionKey.MigrationCases* once `nswag run`
  // has been re-run against the updated backend. Full View/Add/Edit/Delete/Reassign set,
  // same shape as hasEnrolmentsPermission — a MigrationCase is an operational sibling of
  // Enrolment, not an admin config screen.
  hasMigrationCasesPermission(): ModulePermissions {
    return {
      view: this.hasPermission('MigrationCasesView'),
      add: this.hasPermission('MigrationCasesAdd'),
      edit: this.hasPermission('MigrationCasesEdit'),
      delete: this.hasPermission('MigrationCasesDelete'),
      reassign: this.hasPermission('MigrationCasesReassign'),
    };
  }

  // TODO: switch to PermissionKey.VisaProcessTemplatesEdit once `nswag run` has been
  // re-run against the updated backend. Single flat key, same shape as
  // hasDynamicFormsPermission — gates both the VISA Process Templates screen and the
  // Practitioner Tags screen (see docs/09-visa-process-config-module-design.md §C.9).
  hasVisaProcessTemplatesPermission(): ModulePermissions {
    const canEdit = this.hasPermission('VisaProcessTemplatesEdit');
    return {
      view: canEdit,
      add: canEdit,
      edit: canEdit,
      delete: canEdit,
    };
  }

  // TODO: switch to PermissionKey.EmailTemplatesEdit once `nswag run` has been re-run
  // against the updated backend. Single flat key, same shape as hasDynamicFormsPermission —
  // Email Templates management is admin-only, no separate Add/Delete keys.
  hasEmailTemplatesPermission(): ModulePermissions {
    const canEdit = this.hasPermission('EmailTemplatesEdit');
    return {
      view: canEdit,
      add: canEdit,
      edit: canEdit,
      delete: canEdit,
    };
  }

  // Same flat-key shape as hasEmailTemplatesPermission — gates the admin Invoice
  // Template management screen and the sent-invoice ledger. Sending an individual
  // invoice from the Enrolment Form step doesn't use this — that's EnrolmentsEdit.
  hasInvoiceTemplatesPermission(): ModulePermissions {
    const canEdit = this.hasPermission('InvoiceTemplatesEdit');
    return {
      view: canEdit,
      add: canEdit,
      edit: canEdit,
      delete: canEdit,
    };
  }

  // TODO: switch these string literals to PermissionKey.Tasks* once `nswag run` has
  // been re-run against the updated backend — same TODO shape as hasDepartmentsPermission.
  hasTasksPermission(): TaskPermissions {
    return {
      view: this.hasPermission('TasksView'),
      add: this.hasPermission('TasksAdd'),
      edit: this.hasPermission('TasksEdit'),
      viewAll: this.hasPermission('TasksViewAll'),
    };
  }
}
