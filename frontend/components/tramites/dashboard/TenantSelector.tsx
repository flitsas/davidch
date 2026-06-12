"use client";

import { useEffect, useState } from "react";
import type { DashboardTenantOption } from "@/lib/tramites/dashboard-types";

type Props = {
  value: string;
  onChange: (tenantId: string) => void;
};

export function TenantSelector({ value, onChange }: Props) {
  const [options, setOptions] = useState<DashboardTenantOption[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void fetch("/api/v1/admin/companies/index?page=1&pageSize=100", { credentials: "include" })
      .then((res) => (res.ok ? res.json() : { items: [] }))
      .then((data: { items: { tenantId: string; legalName: string }[] }) => {
        setOptions(
          data.items.map((c) => ({
            tenantId: c.tenantId,
            label: c.legalName,
          })),
        );
      })
      .catch(() => setOptions([]))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <p className="text-sm text-flit-text-secondary">Cargando compañías…</p>;
  }

  return (
    <label className="flex flex-col gap-1 text-sm">
      <span className="font-semibold text-flit-text-secondary">Compañía</span>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="min-h-11 rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 text-flit-text-primary"
      >
        <option value="">Todas las compañías</option>
        {options.map((o) => (
          <option key={o.tenantId} value={o.tenantId}>
            {o.label}
          </option>
        ))}
      </select>
    </label>
  );
}
