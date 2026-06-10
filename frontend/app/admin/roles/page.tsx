import Link from "next/link";
import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import type { RoleSummary } from "@/lib/admin/types";
import { CreateRoleForm } from "@/components/admin/CreateRoleForm";

export default async function AdminRolesPage() {
  const session = await getSessionOrRedirect();
  if (!hasPermission(session, "roles:read")) redirect("/");

  const res = await apiFetch("/api/roles");
  const roles: RoleSummary[] = res.ok ? await res.json() : [];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Roles</h1>
      {hasPermission(session, "roles:create") && <CreateRoleForm />}
      <ul className="divide-y divide-zinc-200 rounded border border-zinc-200 bg-white">
        {roles.map((role) => (
          <li key={role.id} className="flex items-center justify-between px-4 py-3 text-sm">
            <div>
              <span className="font-medium">{role.name}</span>
              {role.isSystem && (
                <span className="ml-2 text-xs text-zinc-500">(sistema)</span>
              )}
            </div>
            {!role.isSystem && hasPermission(session, "roles:update") && (
              <Link href={`/admin/roles/${role.id}`} className="text-zinc-700 underline">
                Editar permisos
              </Link>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
