"use client";

import { useEffect, useState } from "react";
import {
  fetchCompanyConfig,
  saveCompanyConfig,
} from "@/lib/admin/companies-config-api";
import { GradientButton } from "@/components/flit/Button";

type SignatureConfig = {
  sellerSignatureType: number;
  buyerSignatureType: number;
  vaultEnabled: boolean;
};

const signatureOptions = [
  { value: 0, label: "ID digital", api: "DigitalId" },
  { value: 1, label: "En pantalla", api: "OnScreen" },
  { value: 2, label: "Preasignada", api: "Preassigned" },
];

export function ConfigEmpresaTab({ companyId }: { companyId: string }) {
  const [config, setConfig] = useState<SignatureConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void fetchCompanyConfig<SignatureConfig>(companyId, "signatures").then((data) => {
      setConfig(
        data ?? {
          sellerSignatureType: 1,
          buyerSignatureType: 1,
          vaultEnabled: false,
        }
      );
      setLoading(false);
    });
  }, [companyId]);

  async function onSave() {
    if (!config) return;
    setSaving(true);
    const seller = signatureOptions.find((o) => o.value === config.sellerSignatureType);
    const buyer = signatureOptions.find((o) => o.value === config.buyerSignatureType);
    const ok = await saveCompanyConfig(companyId, "signatures", {
      sellerSignatureType: seller?.api ?? "OnScreen",
      buyerSignatureType: buyer?.api ?? "OnScreen",
      vaultEnabled: config.vaultEnabled,
    });
    setSaving(false);
    setMessage(ok ? "Firmas guardadas." : "Error al guardar firmas.");
  }

  if (loading || !config) {
    return <p className="text-sm text-flit-text-secondary">Cargando…</p>;
  }

  return (
    <div className="space-y-4">
      <div>
        <label className="mb-1 block text-sm font-medium">Firma vendedor</label>
        <select
          className="w-full max-w-md rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
          value={config.sellerSignatureType}
          onChange={(e) =>
            setConfig({ ...config, sellerSignatureType: Number(e.target.value) })
          }
        >
          {signatureOptions.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium">Firma comprador</label>
        <select
          className="w-full max-w-md rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
          value={config.buyerSignatureType}
          onChange={(e) =>
            setConfig({ ...config, buyerSignatureType: Number(e.target.value) })
          }
        >
          {signatureOptions.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={config.vaultEnabled}
          onChange={(e) => setConfig({ ...config, vaultEnabled: e.target.checked })}
        />
        Baúl documental habilitado
      </label>
      {message && <p className="text-sm text-flit-text-secondary">{message}</p>}
      <GradientButton type="button" onClick={onSave} disabled={saving}>
        {saving ? "Guardando…" : "Guardar firmas"}
      </GradientButton>
    </div>
  );
}
