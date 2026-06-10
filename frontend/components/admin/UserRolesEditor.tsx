"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { RoleConflictWarning, RoleSummary } from "@/lib/admin/types";

export function UserRolesEditor({
  userId,
  userEmail,
  currentRoleIds,
  roles,
}: {
  userId: string;
  userEmail: string;
  currentRoleIds: string[];
  roles: RoleSummary[];
}) {
  const router = useRouter();
  const [selected, setSelected] = useState<string[]>(currentRoleIds);
  const [confirm, setConfirm] = useState(false);
  const [warnings, setWarnings] = useState<RoleConflictWarning[]>([]);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function toggle(id: string) {
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((r) => r !== id) : [...prev, id]
    );
    setWarnings([]);
    setPending(false);
    setConfirm(false);
  }

  async function save() {
    setError(null);
    const res = await fetch(`/api/users/${userId}/roles`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ role_ids: selected, confirm }),
      credentials: "include",
    });

    if (res.status === 200) {
      const body = await res.json();
      if (body.pending) {
        setWarnings(body.warnings ?? []);
        setPending(true);
        return;
      }
    }

    if (!res.ok && res.status !== 204) {
      setError("No se pudieron actualizar los roles");
      return;
    }

    setWarnings([]);
    setPending(false);
    setConfirm(false);
    router.refresh();
  }

  return (
    <details className="rounded border border-zinc-100 p-3 text-sm">
      <summary className="cursor-pointer font-medium">{userEmail}</summary>
      <div className="mt-2 flex flex-wrap gap-2">
        {roles
          .filter((r) => !r.isSystem)
          .map((role) => (
            <label key={role.id} className="flex items-center gap-1">
              <input
                type="checkbox"
                checked={selected.includes(role.id)}
                onChange={() => toggle(role.id)}
              />
              {role.name}
            </label>
          ))}
      </div>
      {warnings.length > 0 && (
        <ul className="mt-2 list-disc pl-5 text-amber-800">
          {warnings.map((w, i) => (
            <li key={i}>{w.message}</li>
          ))}
        </ul>
      )}
      {pending && (
        <label className="mt-2 flex items-center gap-2">
          <input type="checkbox" checked={confirm} onChange={(e) => setConfirm(e.target.checked)} />
          Confirmo aplicar roles con advertencias
        </label>
      )}
      {error && <p className="mt-2 text-red-600">{error}</p>}
      <button
        type="button"
        onClick={save}
        className="mt-2 rounded border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-50"
      >
        Guardar roles
      </button>
    </details>
  );
}
