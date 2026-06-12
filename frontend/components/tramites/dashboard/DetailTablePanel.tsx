"use client";

import { tramiteStatusLabel } from "@/lib/tramites/api";
import type { DashboardCategoryKey, DashboardDetailRow } from "@/lib/tramites/dashboard-types";

const CATEGORY_LABELS: Record<DashboardCategoryKey, string> = {
  matriculas: "Matrículas",
  traspasos: "Traspasos",
  otros: "Otros trámites",
};

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("es-CO", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

type Props = {
  category: DashboardCategoryKey;
  rows: DashboardDetailRow[];
  totalCount: number;
  loading: boolean;
  onExport: () => void;
};

export function DetailTablePanel({ category, rows, totalCount, loading, onExport }: Props) {
  return (
    <div className="flex h-full flex-col" data-testid="dashboard-detail-table">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-sm font-semibold text-flit-text-brand">
          Detalle — {CATEGORY_LABELS[category]}
        </h2>
        <button
          type="button"
          onClick={onExport}
          className="flit-focus-ring rounded-flit-pill border border-flit-border-soft bg-flit-bg-card px-4 py-2 text-sm font-semibold text-flit-text-primary hover:bg-flit-bg-modal"
        >
          Exportar Excel
        </button>
      </div>

      {loading ? (
        <p className="text-sm text-flit-text-secondary">Cargando detalle…</p>
      ) : rows.length === 0 ? (
        <p className="text-sm text-flit-text-secondary">Sin registros en esta categoría.</p>
      ) : (
        <div className="overflow-x-auto rounded-flit-md border border-flit-border-soft">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-flit-bg-table-header text-flit-text-secondary">
              <tr>
                <th className="px-3 py-2 font-semibold">ID</th>
                <th className="px-3 py-2 font-semibold">Radicación</th>
                <th className="px-3 py-2 font-semibold">Estado</th>
                <th className="px-3 py-2 font-semibold">Placa</th>
                <th className="px-3 py-2 font-semibold">Propietario</th>
                <th className="px-3 py-2 font-semibold">Actualización</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.procedureInstanceId} className="border-t border-flit-border-soft">
                  <td className="px-3 py-2 font-mono text-xs">{row.id}</td>
                  <td className="px-3 py-2">{formatDate(row.radicatedAt)}</td>
                  <td className="px-3 py-2">{tramiteStatusLabel(row.status)}</td>
                  <td className="px-3 py-2">{row.plate}</td>
                  <td className="px-3 py-2">{row.ownerName}</td>
                  <td className="px-3 py-2">{formatDate(row.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!loading && totalCount > rows.length ? (
        <p className="mt-2 text-xs text-flit-text-secondary">
          Mostrando {rows.length} de {totalCount} registros.
        </p>
      ) : null}
    </div>
  );
}
