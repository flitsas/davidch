"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { TenantSummary } from "@/lib/admin/types";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

type CreateMode = "create" | "link";

type TrafficAuthorityCatalogItem = {
  code: string;
  name: string;
  region: string;
};

export function OtCreateForm({ tenants }: { tenants: TenantSummary[] }) {
  const router = useRouter();
  const [mode, setMode] = useState<CreateMode>("create");
  const [catalog, setCatalog] = useState<TrafficAuthorityCatalogItem[]>([]);
  const [divipolCode, setDivipolCode] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [slug, setSlug] = useState("");
  const [tenantId, setTenantId] = useState(tenants[0]?.id ?? "");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    void fetch("/api/v1/admin/ot/traffic-authority-catalog", { credentials: "include" })
      .then(async (res) => {
        if (!res.ok) return;
        const items = (await res.json()) as TrafficAuthorityCatalogItem[];
        setCatalog(items);
        if (items.length > 0) {
          setDivipolCode((current) => current || items[0]!.code);
        }
      })
      .catch(() => {});
  }, []);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const body: Record<string, unknown> = {
        mode,
        divipol_code: divipolCode.trim(),
        display_name: displayName.trim(),
        status: "Active",
      };

      if (mode === "create") {
        body.slug = slug.trim().toLowerCase();
      } else {
        body.tenant_id = tenantId;
      }

      const res = await fetch("/api/v1/admin/ot", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo crear el OT");
        return;
      }

      const created = (await res.json()) as { id: string };
      router.push(`/admin/ot/${created.id}?tab=perfil`);
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
          Crear nuevo tenant OT
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
        <div>
          <label
            htmlFor="divipol_code"
            className="mb-1 block text-sm font-semibold text-flit-text-primary"
          >
            Organismo de tránsito (DIVIPOL)
          </label>
          <select
            id="divipol_code"
            name="divipol_code"
            className="flit-focus-ring w-full rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 py-2 text-sm"
            value={divipolCode}
            onChange={(e) => {
              const code = e.target.value;
              setDivipolCode(code);
              const item = catalog.find((c) => c.code === code);
              if (item && !displayName.trim()) {
                setDisplayName(item.name);
              }
            }}
            required
          >
            <option value="">Seleccione…</option>
            {catalog.map((item) => (
              <option key={item.code} value={item.code}>
                {item.name} ({item.code})
              </option>
            ))}
          </select>
          <p className="mt-1 text-xs text-flit-text-secondary">
            Debe coincidir con el organismo habilitado en la configuración de la compañía.
          </p>
        </div>
        <FlitInput
          label="Nombre para mostrar"
          name="display_name"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          required
        />
      </div>

      {mode === "create" ? (
        <FlitInput
          label="Slug del tenant"
          name="tenant_slug"
          hint="Identificador único en minúsculas, p. ej. ot-bogota"
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
        {loading ? "Creando…" : "Crear OT"}
      </GradientButton>
    </form>
  );
}
