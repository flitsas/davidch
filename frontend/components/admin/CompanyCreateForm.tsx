"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { TenantSummary } from "@/lib/admin/types";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

type CreateMode = "create" | "link";

export function CompanyCreateForm({ tenants }: { tenants: TenantSummary[] }) {
  const router = useRouter();
  const [mode, setMode] = useState<CreateMode>("create");
  const [nit, setNit] = useState("");
  const [legalName, setLegalName] = useState("");
  const [slug, setSlug] = useState("");
  const [tenantId, setTenantId] = useState(tenants[0]?.id ?? "");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const body: Record<string, unknown> = {
        mode,
        nit,
        legal_name: legalName,
        status: "Active",
      };

      if (mode === "create") {
        body.slug = slug.trim().toLowerCase();
      } else {
        body.tenant_id = tenantId;
      }

      const res = await fetch("/api/v1/admin/companies", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo crear la compañía");
        return;
      }

      const created = (await res.json()) as { id: string };
      router.push(`/admin/companies/${created.id}?tab=config-empresa`);
      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      <fieldset className="space-y-3">
        <legend className="text-sm font-semibold text-flit-text-brand">
          Modo de aprovisionamiento
        </legend>
        <label className="flex items-center gap-2 text-sm text-flit-text-primary">
          <input
            type="radio"
            name="mode"
            checked={mode === "create"}
            onChange={() => setMode("create")}
          />
          Crear nuevo tenant B2B
        </label>
        <label className="flex items-center gap-2 text-sm text-flit-text-primary">
          <input
            type="radio"
            name="mode"
            checked={mode === "link"}
            onChange={() => setMode("link")}
          />
          Vincular tenant existente
        </label>
      </fieldset>

      <div className="grid gap-4 sm:grid-cols-2">
        <FlitInput
          label="NIT"
          name="nit"
          value={nit}
          onChange={(e) => setNit(e.target.value)}
          required
        />
        <FlitInput
          label="Razón social"
          name="legal_name"
          value={legalName}
          onChange={(e) => setLegalName(e.target.value)}
          required
        />
      </div>

      {mode === "create" ? (
        <FlitInput
          label="Slug del tenant"
          name="tenant_slug"
          hint="Identificador único en minúsculas, p. ej. acme-transport"
          value={slug}
          onChange={(e) => setSlug(e.target.value)}
          required
        />
      ) : (
        <div>
          <label className="mb-1 block text-sm font-medium text-flit-text-secondary">
            Tenant existente
          </label>
          <select
            className="flit-focus-ring w-full rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 py-2 text-sm"
            value={tenantId}
            onChange={(e) => setTenantId(e.target.value)}
            required
          >
            {tenants.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name} ({t.slug})
              </option>
            ))}
          </select>
        </div>
      )}

      {error && (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      )}

      <GradientButton type="submit" disabled={loading}>
        {loading ? "Creando…" : "Crear compañía"}
      </GradientButton>
    </form>
  );
}
