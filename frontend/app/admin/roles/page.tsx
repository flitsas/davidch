import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import type { RoleSummary } from "@/lib/admin/types";
import { CreateRoleForm } from "@/components/admin/CreateRoleForm";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default async function AdminRolesPage() {
  const session = await getSessionOrRedirect();
  if (!hasPermission(session, "roles:read")) redirect("/");

  const res = await apiFetch("/api/roles");
  const roles: RoleSummary[] = res.ok ? await res.json() : [];

  return (
    <>
      <PageHeaderCard title="Roles" subtitle="Permisos y políticas de acceso" />
      {hasPermission(session, "roles:create") && <CreateRoleForm />}
      <FlitCard className="overflow-hidden p-0">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Roles del tenant</h2>
        </div>
        <ul className="divide-y divide-flit-border-soft">
          {roles.map((role) => (
            <li
              key={role.id}
              className="flex flex-wrap items-center justify-between gap-3 px-6 py-4 text-sm"
            >
              <div>
                <span className="font-semibold text-flit-text-primary">{role.name}</span>
                {role.isSystem && (
                  <span className="ml-2 text-xs text-flit-text-muted">(sistema)</span>
                )}
              </div>
              {!role.isSystem && hasPermission(session, "roles:update") && (
                <FlitLink href={`/admin/roles/${role.id}`}>Editar permisos</FlitLink>
              )}
            </li>
          ))}
        </ul>
      </FlitCard>
    </>
  );
}
