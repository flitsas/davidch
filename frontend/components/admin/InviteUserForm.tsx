"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { RoleSummary, TenantSummary } from "@/lib/admin/types";
import { FlitCard } from "@/components/flit/Card";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

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
    <FlitCard>
      <form onSubmit={onSubmit} className="space-y-4">
        <h2 className="text-lg font-semibold text-flit-text-brand">Invitar usuario</h2>
        {isSuperAdmin && (
          <div className="space-y-2">
            <label htmlFor="tenant" className="block text-sm font-semibold text-flit-text-primary">
              Tenant
            </label>
            <select
              id="tenant"
              name="tenant"
              className="flit-focus-ring h-12 w-full rounded-[10px] border border-flit-border-input bg-flit-bg-card px-4 text-sm text-flit-text-primary"
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
          </div>
        )}
        <FlitInput
          label="Correo"
          name="email"
          id="invite-email"
          type="email"
          placeholder="correo@empresa.com…"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="off"
          spellCheck={false}
        />
        <fieldset>
          <legend className="mb-2 text-sm font-semibold text-flit-text-primary">Roles</legend>
          <div className="flex flex-wrap gap-3">
            {roles
              .filter((r) => !r.isSystem)
              .map((role) => {
                const selected = roleIds.includes(role.id);
                return (
                  <label
                    key={role.id}
                    className={[
                      "flit-focus-ring flex cursor-pointer items-center gap-2 rounded-flit-md border px-4 py-2 text-sm font-medium transition-colors duration-[var(--flit-duration-fast)]",
                      selected
                        ? "border-flit-success bg-flit-success/10 text-flit-success"
                        : "border-flit-border-soft bg-flit-bg-card text-flit-text-primary hover:bg-flit-bg-modal",
                    ].join(" ")}
                  >
                    <input
                      type="checkbox"
                      className="sr-only"
                      checked={selected}
                      onChange={() => toggleRole(role.id)}
                    />
                    {role.name}
                  </label>
                );
              })}
          </div>
        </fieldset>
        {error && (
          <p role="alert" className="text-sm text-flit-danger">
            {error}
          </p>
        )}
        <GradientButton type="submit" disabled={loading} className="min-h-10 px-6 text-sm">
          {loading ? "Enviando…" : "Enviar invitación"}
        </GradientButton>
      </form>
    </FlitCard>
  );
}
