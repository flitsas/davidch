"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { OtDetail } from "@/lib/admin/ot-types";
import { otStatusApiValue } from "@/lib/admin/ot-api";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

export function PerfilTab({ ot }: { ot: OtDetail }) {
  const router = useRouter();
  const [divipolCode, setDivipolCode] = useState(ot.divipolCode);
  const [displayName, setDisplayName] = useState(ot.displayName);
  const [status, setStatus] = useState(ot.status);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const patchRes = await fetch(`/api/v1/admin/ot/${ot.id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({
          divipol_code: divipolCode.trim(),
          display_name: displayName.trim(),
        }),
      });

      if (!patchRes.ok) {
        const data = await patchRes.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo actualizar el OT");
        return;
      }

      if (status !== ot.status) {
        const statusRes = await fetch(`/api/v1/admin/ot/${ot.id}/status`, {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ status: otStatusApiValue(status) }),
        });

        if (!statusRes.ok) {
          const data = await statusRes.json().catch(() => ({}));
          setError(data.message ?? data.code ?? "No se pudo actualizar el estado");
          return;
        }
      }

      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <FlitInput
          label="Código DIVIPOL"
          name="divipol_code"
          value={divipolCode}
          onChange={(e) => setDivipolCode(e.target.value)}
          required
        />
        <FlitInput
          label="Nombre para mostrar"
          name="display_name"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          required
        />
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-flit-text-secondary">Estado</label>
        <select
          className="flit-focus-ring w-full max-w-xs rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 py-2 text-sm text-flit-text-primary"
          value={status}
          onChange={(e) => setStatus(Number(e.target.value) as 0 | 1)}
        >
          <option value={0}>Activo</option>
          <option value={1}>Suspendido</option>
        </select>
      </div>

      <p className="text-sm text-flit-text-secondary">
        Tenant vinculado: {ot.tenant.name} ({ot.tenant.slug})
      </p>

      {error && (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      )}

      <GradientButton type="submit" disabled={loading}>
        {loading ? "Guardando…" : "Guardar perfil"}
      </GradientButton>
    </form>
  );
}
