"use client";

import { useEffect, useState } from "react";
import {
  fetchCompanyConfig,
  saveCompanyConfig,
} from "@/lib/admin/companies-config-api";
import { GradientButton } from "@/components/flit/Button";

type MatriculaConfig = {
  allowNewVehicleFiling: boolean;
  allowMiscProcedures: boolean;
};

export function MatriculaTab({ companyId }: { companyId: string }) {
  const [config, setConfig] = useState<MatriculaConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void fetchCompanyConfig<MatriculaConfig>(companyId, "matricula").then((data) => {
      setConfig(data ?? { allowNewVehicleFiling: false, allowMiscProcedures: false });
      setLoading(false);
    });
  }, [companyId]);

  async function onSave() {
    if (!config) return;
    setSaving(true);
    setMessage(null);
    const ok = await saveCompanyConfig(companyId, "matricula", config);
    setMessage(ok ? "Configuración guardada." : "No se pudo guardar.");
    setSaving(false);
  }

  if (loading || !config) {
    return <p className="text-sm text-flit-text-secondary">Cargando…</p>;
  }

  return (
    <div className="space-y-4">
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={config.allowNewVehicleFiling}
          onChange={(e) =>
            setConfig({ ...config, allowNewVehicleFiling: e.target.checked })
          }
        />
        Permitir radicación de vehículos nuevos
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={config.allowMiscProcedures}
          onChange={(e) =>
            setConfig({ ...config, allowMiscProcedures: e.target.checked })
          }
        />
        Permitir trámites misceláneos
      </label>
      {message && <p className="text-sm text-flit-text-secondary">{message}</p>}
      <GradientButton type="button" onClick={onSave} disabled={saving}>
        {saving ? "Guardando…" : "Guardar matrícula"}
      </GradientButton>
    </div>
  );
}
