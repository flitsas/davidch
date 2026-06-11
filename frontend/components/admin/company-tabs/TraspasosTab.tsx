"use client";

import { useEffect, useState } from "react";
import type { UserSummary } from "@/lib/admin/types";
import {
  fetchCompanyConfig,
  saveCompanyConfig,
} from "@/lib/admin/companies-config-api";
import { GradientButton } from "@/components/flit/Button";

type TraspasoConfig = { onlyOwnVehicles: boolean };

export function TraspasosTab({ companyId }: { companyId: string }) {
  const [config, setConfig] = useState<TraspasoConfig | null>(null);
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      const [cfg, usersRes] = await Promise.all([
        fetchCompanyConfig<TraspasoConfig>(companyId, "traspasos"),
        fetch("/api/users", { credentials: "include" }),
      ]);
      setConfig(cfg ?? { onlyOwnVehicles: false });
      if (usersRes.ok) {
        setUsers((await usersRes.json()) as UserSummary[]);
      }
      setLoading(false);
    }
    void load();
  }, [companyId]);

  async function onSaveConfig() {
    if (!config) return;
    setSaving(true);
    const ok = await saveCompanyConfig(companyId, "traspasos", config);
    setMessage(ok ? "Traspasos guardados." : "Error al guardar traspasos.");
    setSaving(false);
  }

  async function onAddExceptions() {
    if (selected.length === 0) return;
    setSaving(true);
    const res = await fetch(
      `/api/v1/admin/companies/${companyId}/exceptions/batch`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ user_ids: selected }),
      }
    );
    setMessage(res.ok ? "Usuarios añadidos a lista blanca." : "Error en lista blanca.");
    setSelected([]);
    setSaving(false);
  }

  if (loading || !config) {
    return <p className="text-sm text-flit-text-secondary">Cargando…</p>;
  }

  return (
    <div className="space-y-6">
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={config.onlyOwnVehicles}
          onChange={(e) =>
            setConfig({ ...config, onlyOwnVehicles: e.target.checked })
          }
        />
        Solo vehículos propios de la compañía
      </label>
      <GradientButton type="button" onClick={onSaveConfig} disabled={saving}>
        Guardar traspasos
      </GradientButton>

      <div>
        <h3 className="mb-2 text-sm font-semibold text-flit-text-brand">
          Lista blanca de usuarios
        </h3>
        <div className="max-h-48 space-y-2 overflow-y-auto rounded-flit-md border border-flit-border-soft p-3">
          {users.map((user) => (
            <label key={user.id} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={selected.includes(user.id)}
                onChange={() =>
                  setSelected((prev) =>
                    prev.includes(user.id)
                      ? prev.filter((id) => id !== user.id)
                      : [...prev, user.id]
                  )
                }
              />
              {user.email}
            </label>
          ))}
        </div>
        <GradientButton
          type="button"
          className="mt-3"
          onClick={onAddExceptions}
          disabled={saving || selected.length === 0}
        >
          Añadir a excepciones
        </GradientButton>
      </div>

      {message && <p className="text-sm text-flit-text-secondary">{message}</p>}
    </div>
  );
}
