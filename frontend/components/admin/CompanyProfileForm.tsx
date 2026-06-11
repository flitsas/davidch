"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import type { CompanyDetail } from "@/lib/admin/companies-types";
import { companyStatusApiValue } from "@/lib/admin/companies-api";
import { FlitButton, GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

export function CompanyProfileForm({ company }: { company: CompanyDetail }) {
  const router = useRouter();
  const [nit, setNit] = useState(company.nit);
  const [legalName, setLegalName] = useState(company.legalName);
  const [status, setStatus] = useState(company.status);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const patchRes = await fetch(`/api/v1/admin/companies/${company.id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ nit, legal_name: legalName }),
      });

      if (!patchRes.ok) {
        const data = await patchRes.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo actualizar la compañía");
        return;
      }

      if (status !== company.status) {
        const statusRes = await fetch(`/api/v1/admin/companies/${company.id}/status`, {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ status: companyStatusApiValue(status) }),
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

      <div>
        <label className="mb-1 block text-sm font-medium text-flit-text-secondary">Estado</label>
        <select
          className="flit-focus-ring w-full max-w-xs rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 py-2 text-sm text-flit-text-primary"
          value={status}
          onChange={(e) => setStatus(Number(e.target.value) as 0 | 1)}
        >
          <option value={0}>Activa</option>
          <option value={1}>Suspendida</option>
        </select>
      </div>

      {error && (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      )}

      <GradientButton type="submit" disabled={loading}>
        {loading ? "Guardando…" : "Guardar datos generales"}
      </GradientButton>
    </form>
  );
}
