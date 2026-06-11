import { ReactNode } from "react";
import { hasPermission, MeResponse, PermissionScope } from "@/lib/auth/permissions";

export function Can({
  permission,
  scope = "Tenant",
  session,
  children,
}: {
  permission: string;
  scope?: PermissionScope;
  session: MeResponse;
  children: ReactNode;
}) {
  if (!hasPermission(session, permission, scope)) return null;
  return <>{children}</>;
}
