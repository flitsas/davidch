"use client";

import { useEffect, useState } from "react";
import {
  fetchCompanyConfig,
  saveCompanyConfig,
} from "@/lib/admin/companies-config-api";
import { GradientButton } from "@/components/flit/Button";

type NotificationConfig = {
  channel: number;
  notificationTarget: number;
};

type PaymentConfig = {
  allowFlitGateway: boolean;
  allowOt: boolean;
  allowOther: boolean;
};

type RuntConfig = {
  primaryProvider: number;
  secondaryProvider: number;
  failoverTimeoutMs: number;
};

type TrafficItem = {
  authorityCode: string;
  name: string;
  region: string | null;
  isEnabled: boolean;
};

type TrafficResponse = {
  items: TrafficItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

const channelOptions = [
  { value: 0, label: "SMTP FLIT", api: "FlitSmtp" },
  { value: 1, label: "API cliente", api: "ClientApi" },
];

const targetOptions = [
  { value: 0, label: "Comprador", api: "Buyer" },
  { value: 1, label: "Radicador", api: "Filer" },
  { value: 2, label: "Ninguno", api: "None" },
];

const providerOptions = [
  { value: 0, label: "Verifik", api: "Verifik" },
  { value: 1, label: "Intempo", api: "Intempo" },
];

export function ContingenciaTab({ companyId }: { companyId: string }) {
  const [notifications, setNotifications] = useState<NotificationConfig | null>(null);
  const [payments, setPayments] = useState<PaymentConfig | null>(null);
  const [runt, setRunt] = useState<RuntConfig | null>(null);
  const [traffic, setTraffic] = useState<TrafficItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      const [n, p, r, t] = await Promise.all([
        fetchCompanyConfig<NotificationConfig>(companyId, "notifications"),
        fetchCompanyConfig<PaymentConfig>(companyId, "payments"),
        fetchCompanyConfig<RuntConfig>(companyId, "runt"),
        fetch(
          `/api/v1/admin/companies/${companyId}/traffic-authorities?page=1&pageSize=50`,
          { credentials: "include" }
        ),
      ]);
      setNotifications(n ?? { channel: 0, notificationTarget: 1 });
      setPayments(
        p ?? { allowFlitGateway: false, allowOt: false, allowOther: false }
      );
      setRunt(
        r ?? { primaryProvider: 0, secondaryProvider: 1, failoverTimeoutMs: 4000 }
      );
      if (t.ok) {
        const body = (await t.json()) as TrafficResponse;
        setTraffic(body.items);
      }
      setLoading(false);
    }
    void load();
  }, [companyId]);

  async function saveNotifications() {
    if (!notifications) return;
    setSaving(true);
    const channel = channelOptions.find((o) => o.value === notifications.channel);
    const target = targetOptions.find(
      (o) => o.value === notifications.notificationTarget
    );
    const ok = await saveCompanyConfig(companyId, "notifications", {
      channel: channel?.api ?? "FlitSmtp",
      notificationTarget: target?.api ?? "Filer",
    });
    setMessage(ok ? "Notificaciones guardadas." : "Error en notificaciones.");
    setSaving(false);
  }

  async function savePayments() {
    if (!payments) return;
    setSaving(true);
    const ok = await saveCompanyConfig(companyId, "payments", payments);
    setMessage(ok ? "Recaudo guardado." : "Error en recaudo.");
    setSaving(false);
  }

  async function saveRunt() {
    if (!runt) return;
    setSaving(true);
    const primary = providerOptions.find((o) => o.value === runt.primaryProvider);
    const secondary = providerOptions.find((o) => o.value === runt.secondaryProvider);
    const ok = await saveCompanyConfig(companyId, "runt", {
      primaryProvider: primary?.api ?? "Verifik",
      secondaryProvider: secondary?.api ?? "Intempo",
      failoverTimeoutMs: runt.failoverTimeoutMs,
      providerCredentials: "{}",
    });
    setMessage(ok ? "RUNT guardado." : "Error en RUNT.");
    setSaving(false);
  }

  async function toggleAuthority(code: string, enabled: boolean) {
    setSaving(true);
    const res = await fetch(
      `/api/v1/admin/companies/${companyId}/traffic-authorities`,
      {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({
          updates: [{ authority_code: code, is_enabled: enabled }],
        }),
      }
    );
    if (res.ok) {
      setTraffic((prev) =>
        prev.map((item) =>
          item.authorityCode === code ? { ...item, isEnabled: enabled } : item
        )
      );
      setMessage("Organismo actualizado.");
    } else {
      setMessage("Error al actualizar organismo.");
    }
    setSaving(false);
  }

  if (loading || !notifications || !payments || !runt) {
    return <p className="text-sm text-flit-text-secondary">Cargando…</p>;
  }

  return (
    <div className="space-y-8">
      <section className="space-y-3">
        <h3 className="text-sm font-semibold text-flit-text-brand">Notificaciones</h3>
        <select
          className="w-full max-w-md rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
          value={notifications.channel}
          onChange={(e) =>
            setNotifications({ ...notifications, channel: Number(e.target.value) })
          }
        >
          {channelOptions.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
        <select
          className="w-full max-w-md rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
          value={notifications.notificationTarget}
          onChange={(e) =>
            setNotifications({
              ...notifications,
              notificationTarget: Number(e.target.value),
            })
          }
        >
          {targetOptions.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
        <GradientButton type="button" onClick={saveNotifications} disabled={saving}>
          Guardar notificaciones
        </GradientButton>
      </section>

      <section className="space-y-3">
        <h3 className="text-sm font-semibold text-flit-text-brand">Recaudo</h3>
        {(
          [
            ["allowFlitGateway", "Pasarela FLIT"],
            ["allowOt", "Organismo de tránsito"],
            ["allowOther", "Otros medios"],
          ] as const
        ).map(([key, label]) => (
          <label key={key} className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={payments[key]}
              onChange={(e) =>
                setPayments({ ...payments, [key]: e.target.checked })
              }
            />
            {label}
          </label>
        ))}
        <GradientButton type="button" onClick={savePayments} disabled={saving}>
          Guardar recaudo
        </GradientButton>
      </section>

      <section className="space-y-3">
        <h3 className="text-sm font-semibold text-flit-text-brand">RUNT</h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <select
            className="rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
            value={runt.primaryProvider}
            onChange={(e) =>
              setRunt({ ...runt, primaryProvider: Number(e.target.value) })
            }
          >
            {providerOptions.map((o) => (
              <option key={o.value} value={o.value}>
                Primario: {o.label}
              </option>
            ))}
          </select>
          <select
            className="rounded-flit-md border border-flit-border-soft px-3 py-2 text-sm"
            value={runt.secondaryProvider}
            onChange={(e) =>
              setRunt({ ...runt, secondaryProvider: Number(e.target.value) })
            }
          >
            {providerOptions.map((o) => (
              <option key={o.value} value={o.value}>
                Secundario: {o.label}
              </option>
            ))}
          </select>
        </div>
        <GradientButton type="button" onClick={saveRunt} disabled={saving}>
          Guardar RUNT
        </GradientButton>
      </section>

      <section>
        <h3 className="mb-3 text-sm font-semibold text-flit-text-brand">
          Organismos de tránsito
        </h3>
        <div className="overflow-x-auto rounded-flit-md border border-flit-border-soft">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-flit-bg-table-header text-flit-text-secondary">
              <tr>
                <th className="px-4 py-2">Código</th>
                <th className="px-4 py-2">Nombre</th>
                <th className="px-4 py-2">Habilitado</th>
              </tr>
            </thead>
            <tbody>
              {traffic.map((item) => (
                <tr key={item.authorityCode} className="border-t border-flit-border-soft">
                  <td className="px-4 py-2">{item.authorityCode}</td>
                  <td className="px-4 py-2">{item.name}</td>
                  <td className="px-4 py-2">
                    <input
                      type="checkbox"
                      checked={item.isEnabled}
                      disabled={saving}
                      onChange={(e) =>
                        void toggleAuthority(item.authorityCode, e.target.checked)
                      }
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {message && <p className="text-sm text-flit-text-secondary">{message}</p>}
    </div>
  );
}
