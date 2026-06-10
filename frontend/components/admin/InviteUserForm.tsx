"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { RoleSummary, TenantSummary } from "@/lib/admin/types";

export function InviteUserForm({
  roles,
  tenants,
  isSuperAdmin,
}: {
  roles: RoleSummary[];
  tenants: TenantSummary[];
  isSuperAdmin: boolean;
}) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [tenantId, setTenantId] = useState(tenants[0]?.id ?? "");
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  function toggleRole(id: string) {
    setRoleIds((prev) =>
      prev.includes(id) ? prev.filter((r) => r !== id) : [...prev, id]
    );
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (roleIds.length === 0) {
      setError("Selecciona al menos un rol");
      return;
    }
    if (isSuperAdmin && !tenantId) {
      setError("Selecciona un tenant");
      return;
    }
    setError(null);
    setLoading(true);
    try {
      const body: Record<string, unknown> = { email, role_ids: roleIds };
      if (isSuperAdmin) body.tenant_id = tenantId;

      const res = await fetch("/api/users/invite", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        credentials: "include",
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.code ?? "No se pudo enviar la invitación");
        return;
      }
      setEmail("");
      setRoleIds([]);
      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3 rounded border border-zinc-200 p-4">
      <h2 className="font-medium">Invitar usuario</h2>
      {isSuperAdmin && (
        <select
          className="w-full rounded border border-zinc-300 px-3 py-2 text-sm"
          value={tenantId}
          onChange={(e) => setTenantId(e.target.value)}
          required
        >
          <option value="">Seleccionar tenant…</option>
          {tenants.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </select>
      )}
      <input
        name="email"
        className="w-full rounded border border-zinc-300 px-3 py-2 text-sm"
        type="email"
        placeholder="correo@empresa.com"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      <div className="flex flex-wrap gap-2">
        {roles
          .filter((r) => !r.isSystem)
          .map((role) => (
            <label key={role.id} className="flex items-center gap-1 text-sm">
              <input
                type="checkbox"
                checked={roleIds.includes(role.id)}
                onChange={() => toggleRole(role.id)}
              />
              {role.name}
            </label>
          ))}
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <button
        type="submit"
        disabled={loading}
        className="rounded bg-zinc-900 px-3 py-1.5 text-sm text-white disabled:opacity-50"
      >
        {loading ? "Enviando…" : "Enviar invitación"}
      </button>
    </form>
  );
}
