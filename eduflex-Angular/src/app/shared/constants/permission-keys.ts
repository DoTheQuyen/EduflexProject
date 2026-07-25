/**
 * Mirrors the backend's PermissionKey enum (ShareService/Enums/Permissions/PermissionKeyEnums.cs).
 * Not auto-generated: System.Text.Json serializes C# enums as numbers by default (no
 * JsonStringEnumConverter configured), so NSwag can't produce a useful string-valued TS
 * enum from it - AuthHelperService.hasPermission() needs the literal dotted strings below,
 * not numeric ordinals. Keep this list in sync by hand whenever the backend enum changes.
 */
export const PermissionKeys = {
  ApplicationsView: 'applications.view',
  ApplicationsAdd: 'applications.add',
  ApplicationsEdit: 'applications.edit',
  ApplicationsDelete: 'applications.delete',

  FinanceView: 'finance.view',
  FinanceAdd: 'finance.add',
  FinanceEdit: 'finance.edit',
  FinanceDelete: 'finance.delete',

  CoursePromotionsView: 'coursepromotions.view',
  CoursePromotionsAdd: 'coursepromotions.add',
  CoursePromotionsEdit: 'coursepromotions.edit',
  CoursePromotionsDelete: 'coursepromotions.delete',

  RolesView: 'roles.view',
  RolesAdd: 'roles.add',
  RolesEdit: 'roles.edit',
  RolesDelete: 'roles.delete',

  UsersView: 'users.view',
  UsersAdd: 'users.add',
  UsersEdit: 'users.edit',
  UsersDelete: 'users.delete',
} as const;

export type PermissionKey = typeof PermissionKeys[keyof typeof PermissionKeys];
