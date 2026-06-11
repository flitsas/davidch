"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { PermissionCatalogItem, RoleDetail, RoleSummary } from "@/lib/admin/types";
import { GradientButton, DangerButton, FlitButton } from "@/components/flit/Button";
import { AlertCard } from "@/components/flit/Alert";

const SCOPES = ["Tenant", "Own", "Global"] as const;

export function RolePermissionsEditor({
  role,
  catalog,
  allRoles,
}: {
  role: RoleDetail;
  catalog: PermissionCatalogItem[];
  allRoles: RoleSummary[];
}) {
  const router = useRouter();
  const initial = useMemo(() => {
    const map = new Map<string, string>();
    for (const p of role.permissions) {
      map.set(p.permissionId, p.scope);
    }
    return map;
  }, [role.permissions]);

  const [selected, setSelected] = useState<Map<string, string>>(initial);
  const [message, setMessage] = useState<string | null>(null);
  const [showMigrate, setShowMigrate] = useState(false);
  const [migrationTargetId, setMigrationTargetId] = useState("");
  const [affectedUsers, setAffectedUsers] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);

  function toggle(permId: string, scope: string) {
    setSelected((prev) => {
      const next = new Map(prev);
      if (next.get(permId) === scope) {
        next.delete(permId);
      } else {
        next.set(permId, scope);
      }
      return next;
    });
  }

  function isChecked(permId: string, scope: string) {
    return selected.get(permId) === scope;
  }

  async function save() {
    setMessage(null);
    setSaving(true);
    try {
      const permissions = [...selected.entries()].map(([permission_id, scope]) => ({
        permission_id,
        scope,
      }));
      const res = await fetch(`/api/roles/${role.id}/permissions`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ permissions }),
        credentials: "include",
      });
      if (!res.ok) {
        setMessage("Error al guardar permisos");
        return;
      }
      setMessage("Permisos guardados");
      router.refresh();
    } finally {
      setSaving(false);
    }
  }

  async function deleteRole() {
    const res = await fetch(`/api/roles/${role.id}`, {
      method: "DELETE",
      credentials: "include",
    });
    if (res.status === 409) {
      const body = await res.json();
      setAffectedUsers(body.affected_users ?? null);
      setShowMigrate(true);
      setMessage(`Rol con ${body.affected_users ?? "?"} usuarios. Selecciona un rol de reemplazo.`);
      return;
    }
    if (!res.ok) {
      setMessage("No se pudo eliminar el rol");
      return;
    }
    router.push("/admin/roles");
    router.refresh();
  }

  async function migrate() {
    if (!migrationTargetId) {
      setMessage("Selecciona un rol de reemplazo");
      return;
    }
    const res = await fetch(`/api/roles/${role.id}/migrate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ replacement_role_id: migrationTargetId }),
      credentials: "include",
    });
    if (!res.ok) {
      setMessage("Migración fallida");
      return;
    }
    router.push("/admin/roles");
    router.refresh();
  }

  if (role.isSystem) {
    return <p className="text-sm text-flit-text-secondary">Rol de sistema — no editable.</p>;
  }

  const replacementOptions = allRoles.filter((r) => r.id !== role.id && !r.isSystem);

  return (
    <div className="space-y-6">
      <div className="overflow-x-auto rounded-flit-lg border border-flit-border-soft">
        <table className="min-w-full text-sm">
          <thead className="bg-flit-bg-table-header">
            <tr>
              <th className="px-4 py-3 text-left font-semibold text-flit-text-brand">Permiso</th>
              {SCOPES.map((s) => (
                <th key={s} className="px-4 py-3 text-center font-semibold text-flit-text-brand">
                  {s}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {catalog.map((perm) => (
              <tr key={perm.id} className="border-t border-flit-border-soft bg-flit-bg-card">
                <td className="px-4 py-3">
                  <div className="font-semibold text-flit-text-primary">{perm.key}</div>
                  <div className="text-xs text-flit-text-muted">{perm.description}</div>
                </td>
                {SCOPES.map((scope) => (
                  <td key={scope} className="px-4 py-3 text-center">
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-flit-border-input text-flit-blue focus:ring-flit-border-focus"
                      checked={isChecked(perm.id, scope)}
                      onChange={() => toggle(perm.id, scope)}
                      disabled={perm.type === "Ui" && scope !== "Tenant"}
                      aria-label={`${perm.key} — ${scope}`}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex flex-wrap gap-3">
        <GradientButton
          type="button"
          onClick={save}
          disabled={saving}
          className="min-h-10 px-6 text-sm"
        >
          {saving ? "Guardando…" : "Guardar permisos"}
        </GradientButton>
        <DangerButton type="button" onClick={deleteRole} className="min-h-10 px-6 text-sm">
          Eliminar rol
        </DangerButton>
      </div>
      {showMigrate && (
        <AlertCard variant="warning">
          <div className="flex flex-wrap items-center gap-3">
            <select
              className="flit-focus-ring rounded-[10px] border border-flit-border-input bg-flit-bg-card px-3 py-2 text-sm"
              value={migrationTargetId}
              onChange={(e) => setMigrationTargetId(e.target.value)}
              aria-label="Rol de reemplazo"
            >
              <option value="">Rol de reemplazo…</option>
              {replacementOptions.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
            <FlitButton variant="ghost" onClick={migrate} className="min-h-9 px-4 text-sm">
              Migrar{affectedUsers ? ` (${affectedUsers} usuarios)` : ""}
            </FlitButton>
          </div>
        </AlertCard>
      )}
      {message && (
        <p className="text-sm text-flit-text-secondary" aria-live="polite">
          {message}
        </p>
      )}
    </div>
  );
}
