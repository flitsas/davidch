import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import type { PermissionCatalogItem, RoleDetail, RoleSummary } from "@/lib/admin/types";
import { RolePermissionsEditor } from "@/components/admin/RolePermissionsEditor";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default async function RoleDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const session = await getSessionOrRedirect();
  if (!hasPermission(session, "roles:read")) redirect("/");

  const [roleRes, permsRes, allRolesRes] = await Promise.all([
    apiFetch(`/api/roles/${id}`),
    apiFetch("/api/permissions"),
    apiFetch("/api/roles"),
  ]);

  if (!roleRes.ok) {
    return (
      <FlitCard>
        <p className="text-flit-text-secondary">Rol no encontrado.</p>
        <FlitLink href="/admin/roles" className="mt-4 inline-block">
          Volver a roles
        </FlitLink>
      </FlitCard>
    );
  }

  const role: RoleDetail = await roleRes.json();
  const catalog: PermissionCatalogItem[] = permsRes.ok ? await permsRes.json() : [];
  const allRoles: RoleSummary[] = allRolesRes.ok ? await allRolesRes.json() : [];

  return (
    <>
      <PageHeaderCard
        title={role.name}
        subtitle="Configuración de permisos del rol"
        actions={
          <FlitLink href="/admin/roles" className="text-sm">
            ← Roles
          </FlitLink>
        }
      />
      <FlitCard>
        {hasPermission(session, "roles:update") ? (
          <RolePermissionsEditor role={role} catalog={catalog} allRoles={allRoles} />
        ) : (
          <p className="text-sm text-flit-text-secondary">Solo lectura.</p>
        )}
      </FlitCard>
    </>
  );
}
