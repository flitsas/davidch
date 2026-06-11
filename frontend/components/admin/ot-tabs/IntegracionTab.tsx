"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import type { IntegrationConfig, IntegrationMode } from "@/lib/ot/settings-api";
import { integrationModeLabel } from "@/lib/ot/settings-api";
import { GradientButton } from "@/components/flit/Button";

type Props = {
  otId?: string;
  apiBase: "admin" | "settings";
};

function integrationPath(apiBase: Props["apiBase"], otId?: string): string {
  if (apiBase === "admin") {
    return `/api/v1/admin/ot/${otId}/config/integration`;
  }
  return "/api/v1/ot/settings/config/integration";
}

export function IntegracionTab({ otId, apiBase }: Props) {
  const router = useRouter();
  const [mode, setMode] = useState<IntegrationMode>("Dashboard");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(integrationPath(apiBase, otId), { credentials: "include" });
        if (!res.ok) {
          const data = await res.json().catch(() => ({}));
          if (!cancelled) {
            setError(data.message ?? data.code ?? "No se pudo cargar la integración");
          }
          return;
        }
        const data = (await res.json()) as IntegrationConfig;
        if (!cancelled) {
          setMode(data.integration_mode);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [apiBase, otId]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);

    try {
      const res = await fetch(integrationPath(apiBase, otId), {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ integration_mode: mode }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo guardar la integración");
        return;
      }

      router.refresh();
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <p className="text-sm text-flit-text-secondary">Cargando configuración…</p>;
  }

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      <fieldset className="space-y-3">
        <legend className="text-sm font-semibold text-flit-text-brand">
          Modo de integración (RF02)
        </legend>
        <label className="flex items-center gap-2 text-sm text-flit-text-primary">
          <input
            type="radio"
            name="integration_mode"
            checked={mode === "Dashboard"}
            onChange={() => setMode("Dashboard")}
          />
          {integrationModeLabel("Dashboard")}
        </label>
        <label className="flex items-center gap-2 text-sm text-flit-text-primary">
          <input
            type="radio"
            name="integration_mode"
            checked={mode === "Qx"}
            onChange={() => setMode("Qx")}
          />
          {integrationModeLabel("Qx")}
        </label>
      </fieldset>

      {error && (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      )}

      <GradientButton type="submit" disabled={saving}>
        {saving ? "Guardando…" : "Guardar integración"}
      </GradientButton>
    </form>
  );
}
