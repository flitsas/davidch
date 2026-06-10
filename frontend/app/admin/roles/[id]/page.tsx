import Link from "next/link";
import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSession, hasPermission } from "@/lib/auth/session";
import type { PermissionCatalogItem, RoleDetail } from "@/lib/admin/types";
import { RolePermissionsEditor } from "@/components/admin/RolePermissionsEditor";

export default async function RoleDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const session = await getSession();
  if (!session) redirect("/login");
  if (!hasPermission(session, "roles:read")) redirect("/");

  const [roleRes, permsRes] = await Promise.all([
    apiFetch(`/api/roles/${id}`),
    apiFetch("/api/permissions"),
  ]);

  if (!roleRes.ok) {
    return (
      <main className="p-8">
        <p>Rol no encontrado.</p>
        <Link href="/admin/roles" className="underline">
          Volver
        </Link>
      </main>
    );
  }

  const role: RoleDetail = await roleRes.json();
  const catalog: PermissionCatalogItem[] = permsRes.ok ? await permsRes.json() : [];

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <Link href="/admin/roles" className="text-sm underline">
          ← Roles
        </Link>
        <h1 className="text-2xl font-semibold">{role.name}</h1>
      </div>
      {hasPermission(session, "roles:update") ? (
        <RolePermissionsEditor role={role} catalog={catalog} />
      ) : (
        <p className="text-sm text-zinc-600">Solo lectura.</p>
      )}
    </div>
  );
}
