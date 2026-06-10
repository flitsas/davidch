import { apiFetch } from "./api-client";

export type PermissionScope = "Global" | "Tenant" | "Own";

export type MeResponse = {
  id: string;
  email: string;
  tenantId: string | null;
  roles: string[];
  permissions: { key: string; scope: PermissionScope }[];
  isSuperAdmin: boolean;
};

export async function getSession(): Promise<MeResponse | null> {
  const res = await apiFetch("/api/auth/me");
  if (!res.ok) return null;
  return res.json();
}

export function hasPermission(
  session: MeResponse,
  key: string,
  scope: PermissionScope = "Tenant"
) {
  if (session.isSuperAdmin) return true;
  return session.permissions.some((p) => p.key === key && p.scope === scope);
}
