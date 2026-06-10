import { redirect } from "next/navigation";
import { apiFetch, SessionRevokedError } from "./api-client";

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

export async function getSessionOrRedirect(): Promise<MeResponse> {
  try {
    const session = await getSession();
    if (!session) redirect("/login");
    return session;
  } catch (e) {
    if (e instanceof SessionRevokedError) {
      redirect("/login?reason=session_revoked");
    }
    throw e;
  }
}

export function hasPermission(
  session: MeResponse,
  key: string,
  scope: PermissionScope = "Tenant"
) {
  if (session.isSuperAdmin) return true;
  return session.permissions.some((p) => p.key === key && p.scope === scope);
}
