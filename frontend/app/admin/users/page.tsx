import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import type { RoleSummary, TenantSummary, UserSummary } from "@/lib/admin/types";
import { InviteUserForm } from "@/components/admin/InviteUserForm";
import { UserRolesEditor } from "@/components/admin/UserRolesEditor";
import { UserActions } from "@/components/admin/UserActions";

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
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Usuarios</h1>
      {hasPermission(session, "users:create") && (
        <InviteUserForm roles={roles} tenants={tenants} isSuperAdmin={session.isSuperAdmin} />
      )}
      <section className="space-y-2">
        <h2 className="font-medium">Listado</h2>
        {users.length === 0 ? (
          <p className="text-sm text-zinc-600">No hay usuarios en este tenant.</p>
        ) : (
          users.map((user) => (
            <div key={user.id} className="rounded border border-zinc-200 bg-white p-3">
              <div className="flex items-center justify-between text-sm">
                <span className="font-medium">{user.email}</span>
                <span className="rounded bg-zinc-100 px-2 py-0.5 text-xs">{user.status}</span>
              </div>
              {hasPermission(session, "users:update") && (
                <>
                  <UserActions userId={user.id} userEmail={user.email} />
                  <UserRolesEditor
                    key={`${user.id}-${(user.role_ids ?? []).join(",")}`}
                    userId={user.id}
                    userEmail={user.email}
                    currentRoleIds={user.role_ids ?? []}
                    roles={roles}
                  />
                </>
              )}
            </div>
          ))
        )}
      </section>
    </div>
  );
}
