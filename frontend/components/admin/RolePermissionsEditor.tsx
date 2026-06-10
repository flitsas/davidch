"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { PermissionCatalogItem, RoleDetail } from "@/lib/admin/types";

const SCOPES = ["Tenant", "Own", "Global"] as const;

export function RolePermissionsEditor({
  role,
  catalog,
}: {
  role: RoleDetail;
  catalog: PermissionCatalogItem[];
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
  }

  async function deleteRole() {
    const res = await fetch(`/api/roles/${role.id}`, {
      method: "DELETE",
      credentials: "include",
    });
    if (res.status === 409) {
      const body = await res.json();
      setMessage(`Rol con ${body.affected_users ?? "?"} usuarios. Usa migración.`);
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
    const replacement = prompt("ID del rol de reemplazo:");
    if (!replacement) return;
    const res = await fetch(`/api/roles/${role.id}/migrate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ replacement_role_id: replacement }),
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
    return <p className="text-sm text-zinc-600">Rol de sistema — no editable.</p>;
  }

  return (
    <div className="space-y-4">
      <div className="overflow-x-auto rounded border border-zinc-200">
        <table className="min-w-full text-sm">
          <thead className="bg-zinc-50">
            <tr>
              <th className="px-3 py-2 text-left">Permiso</th>
              {SCOPES.map((s) => (
                <th key={s} className="px-3 py-2 text-center">
                  {s}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {catalog.map((perm) => (
              <tr key={perm.id} className="border-t border-zinc-100">
                <td className="px-3 py-2">
                  <div className="font-medium">{perm.key}</div>
                  <div className="text-xs text-zinc-500">{perm.description}</div>
                </td>
                {SCOPES.map((scope) => (
                  <td key={scope} className="px-3 py-2 text-center">
                    <input
                      type="checkbox"
                      checked={isChecked(perm.id, scope)}
                      onChange={() => toggle(perm.id, scope)}
                      disabled={perm.type === "Ui" && scope !== "Tenant"}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={save}
          className="rounded bg-zinc-900 px-3 py-2 text-sm text-white"
        >
          Guardar permisos
        </button>
        <button
          type="button"
          onClick={deleteRole}
          className="rounded border border-red-300 px-3 py-2 text-sm text-red-700"
        >
          Eliminar rol
        </button>
        <button
          type="button"
          onClick={migrate}
          className="rounded border border-zinc-300 px-3 py-2 text-sm"
        >
          Migrar usuarios
        </button>
      </div>
      {message && <p className="text-sm text-zinc-700">{message}</p>}
    </div>
  );
}
