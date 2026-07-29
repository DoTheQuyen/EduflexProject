import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { PermissionKey } from '@services/api.services';

export interface ModulePermissions {
  view: boolean;
  add: boolean;
  edit: boolean;
  delete: boolean;
  reassign?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthHelperService {
  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    const token = sessionStorage.getItem('authToken');
    return !!token && this.isTokenValid();
  }

  private isTokenValid(): boolean {
    // Add your token validation logic here
    // For now, just return true if token exists
    return true;
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

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.removeItem('authToken');
      sessionStorage.removeItem('userData');
      sessionStorage.removeItem('rememberMe');
      sessionStorage.removeItem('userEmail');
    }
    // Optionally navigate using Router here instead of full reload
  }

  getAuthToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return sessionStorage.getItem('authToken');
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
      delete: this.hasPermission(PermissionKey.UsersDelete)
    };
  }

  hasCoursePromotionsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.CoursePromotionsView),
      add: this.hasPermission(PermissionKey.CoursePromotionsAdd),
      edit: this.hasPermission(PermissionKey.CoursePromotionsEdit),
      delete: this.hasPermission(PermissionKey.CoursePromotionsDelete)
    };
  }

  hasRolesPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.RolesView),
      add: this.hasPermission(PermissionKey.RolesAdd),
      edit: this.hasPermission(PermissionKey.RolesEdit),
      delete: this.hasPermission(PermissionKey.RolesDelete)
    };
  }

  hasEnquiryPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EnquiryView),
      add: this.hasPermission(PermissionKey.EnquiryAdd),
      edit: this.hasPermission(PermissionKey.EnquiryEdit),
      delete: this.hasPermission(PermissionKey.EnquiryDelete)
    };
  }

  hasEducationPartnersPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EducationPartnersView),
      add: this.hasPermission(PermissionKey.EducationPartnersAdd),
      edit: this.hasPermission(PermissionKey.EducationPartnersEdit),
      delete: this.hasPermission(PermissionKey.EducationPartnersDelete)
    };
  }

  hasBusinessPartnersPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.BusinessPartnersView),
      add: this.hasPermission(PermissionKey.BusinessPartnersAdd),
      edit: this.hasPermission(PermissionKey.BusinessPartnersEdit),
      delete: this.hasPermission(PermissionKey.BusinessPartnersDelete)
    };
  }

  hasEnrolmentsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.EnrolmentsView),
      add: this.hasPermission(PermissionKey.EnrolmentsAdd),
      edit: this.hasPermission(PermissionKey.EnrolmentsEdit),
      delete: this.hasPermission(PermissionKey.EnrolmentsDelete),
      reassign: this.hasPermission(PermissionKey.EnrolmentsReassign)
    };
  }

  hasFinancePermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.FinanceView),
      add: this.hasPermission(PermissionKey.FinanceAdd),
      edit: this.hasPermission(PermissionKey.FinanceEdit),
      delete: this.hasPermission(PermissionKey.FinanceDelete)
    };
  }

    hasStudentsPermission(): ModulePermissions {
    return {
      view: this.hasPermission(PermissionKey.StudentsView),
      add: this.hasPermission(PermissionKey.StudentsAdd),
      edit: this.hasPermission(PermissionKey.StudentsEdit),
      delete: this.hasPermission(PermissionKey.StudentsDelete)
    };
  }

  hasSettingsPermission(): ModulePermissions {
    const canEdit = this.hasPermission(PermissionKey.SettingsEdit);
    return {
      view: canEdit,
      add: false,
      edit: canEdit,
      delete: false
    };
  }
}
