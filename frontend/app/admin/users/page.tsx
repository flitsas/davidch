import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import type { RoleSummary, TenantSummary, UserSummary } from "@/lib/admin/types";
import { InviteUserForm } from "@/components/admin/InviteUserForm";
import { UserRolesEditor } from "@/components/admin/UserRolesEditor";
import { UserActions } from "@/components/admin/UserActions";
import { PageHeaderCard } from "@/components/flit/Card";
import { StatusChip, statusVariantFromUserStatus } from "@/components/flit/Chip";
import { FlitCard } from "@/components/flit/Card";

export default async function AdminUsersPage() {
  const session = await getSessionOrRedirect();
  if (!hasPermission(session, "users:read")) redirect("/");

  const tenantsPromise = session.isSuperAdmin ? apiFetch("/api/tenants") : null;
  const [usersRes, rolesRes, tenantsRes] = await Promise.all([
    apiFetch("/api/users"),
    apiFetch("/api/roles"),
    tenantsPromise,
  ]);

  const users: UserSummary[] = usersRes.ok ? await usersRes.json() : [];
  const roles: RoleSummary[] = rolesRes.ok ? await rolesRes.json() : [];
  const tenants: TenantSummary[] =
    tenantsRes?.ok ? await tenantsRes.json() : [];

  return (
    <>
      <PageHeaderCard title="Usuarios" subtitle="Gestión de colaboradores y accesos" />
      {hasPermission(session, "users:create") && (
        <InviteUserForm roles={roles} tenants={tenants} isSuperAdmin={session.isSuperAdmin} />
      )}
      <FlitCard className="overflow-hidden p-0">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Listado</h2>
        </div>
        {users.length === 0 ? (
          <p className="px-6 py-8 text-sm text-flit-text-secondary">
            No hay usuarios en este tenant.
          </p>
        ) : (
          <ul className="divide-y divide-flit-border-soft">
            {users.map((user) => (
              <li key={user.id} className="px-6 py-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <span className="font-semibold text-flit-text-primary">{user.email}</span>
                  <StatusChip
                    label={user.status}
                    variant={statusVariantFromUserStatus(user.status)}
                  />
                </div>
                {hasPermission(session, "users:update") && (
                  <div className="mt-4 space-y-3">
                    <UserActions userId={user.id} userEmail={user.email} />
                    <UserRolesEditor
                      key={`${user.id}-${(user.role_ids ?? []).join(",")}`}
                      userId={user.id}
                      userEmail={user.email}
                      currentRoleIds={user.role_ids ?? []}
                      roles={roles}
                    />
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </FlitCard>
    </>
  );
}
