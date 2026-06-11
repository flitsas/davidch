"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { RoleConflictWarning, RoleSummary } from "@/lib/admin/types";
import { GradientButton } from "@/components/flit/Button";

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
  const [saving, setSaving] = useState(false);

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
    setSaving(true);
    try {
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
    } finally {
      setSaving(false);
    }
  }

  return (
    <details className="rounded-flit-md border border-flit-border-soft bg-flit-bg-modal/50 p-4 text-sm">
      <summary className="cursor-pointer font-semibold text-flit-text-primary flit-focus-ring rounded-sm">
        Editar roles — {userEmail}
      </summary>
      <fieldset className="mt-4">
        <legend className="sr-only">Roles para {userEmail}</legend>
        <div className="flex flex-wrap gap-2">
          {roles
            .filter((r) => !r.isSystem)
            .map((role) => {
              const checked = selected.includes(role.id);
              return (
                <label
                  key={role.id}
                  className={[
                    "flit-focus-ring flex cursor-pointer items-center gap-2 rounded-flit-md border px-3 py-2 text-sm transition-colors",
                    checked
                      ? "border-flit-success bg-flit-success/10 text-flit-success"
                      : "border-flit-border-soft bg-flit-bg-card text-flit-text-primary",
                  ].join(" ")}
                >
                  <input
                    type="checkbox"
                    className="sr-only"
                    checked={checked}
                    onChange={() => toggle(role.id)}
                  />
                  {role.name}
                </label>
              );
            })}
        </div>
      </fieldset>
      {warnings.length > 0 && (
        <ul className="mt-4 list-disc space-y-1 pl-5 text-flit-warning">
          {warnings.map((w, i) => (
            <li key={i}>{w.message}</li>
          ))}
        </ul>
      )}
      {pending && (
        <label className="mt-4 flex cursor-pointer items-center gap-2 text-flit-text-primary">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-flit-border-input text-flit-blue focus:ring-flit-border-focus"
            checked={confirm}
            onChange={(e) => setConfirm(e.target.checked)}
          />
          Confirmo aplicar roles con advertencias
        </label>
      )}
      {error && (
        <p role="alert" className="mt-4 text-sm text-flit-danger">
          {error}
        </p>
      )}
      <div className="mt-4">
        <GradientButton
          type="button"
          onClick={save}
          disabled={saving}
          className="min-h-9 px-4 text-xs"
        >
          {saving ? "Guardando…" : "Guardar roles"}
        </GradientButton>
      </div>
    </details>
  );
}
