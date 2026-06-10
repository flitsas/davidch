export type UserSummary = {
  id: string;
  email: string;
  status: string;
  role_ids: string[];
  tenantId: string | null;
  createdAt: string;
  activatedAt: string | null;
};

export type TenantSummary = {
  id: string;
  name: string;
  slug: string;
};

export type RoleSummary = {
  id: string;
  name: string;
  isSystem: boolean;
  tenantId: string | null;
  createdAt: string;
};

export type RoleDetail = RoleSummary & {
  permissions: { permissionId: string; key: string; scope: string }[];
};

export type PermissionCatalogItem = {
  id: string;
  key: string;
  type: string;
  module: string | null;
  description: string;
};

export type RoleConflictWarning = {
  type: string;
  message: string;
  role_id?: string;
  other_role_id?: string;
  permission_key?: string;
};
