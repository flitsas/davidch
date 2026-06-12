import { tramiteStatusLabel } from "@/lib/tramites/api";
import type { TramiteIndexItem } from "@/lib/tramites/types";
import { StatusChip } from "@/components/flit/Chip";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("es-CO", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export function TramitesIndexTable({ items }: { items: TramiteIndexItem[] }) {
  if (items.length === 0) {
    return (
      <p className="px-6 py-8 text-sm text-flit-text-secondary" data-testid="tramites-empty">
        Aún no hay trámites registrados.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto" data-testid="tramites-index-table">
      <table className="min-w-full text-left text-sm">
        <thead className="bg-flit-bg-table-header text-flit-text-secondary">
          <tr>
            <th className="px-6 py-3 font-semibold">Referencia</th>
            <th className="px-6 py-3 font-semibold">Tipo</th>
            <th className="px-6 py-3 font-semibold">OT</th>
            <th className="px-6 py-3 font-semibold">Vehículo</th>
            <th className="px-6 py-3 font-semibold">Estado</th>
            <th className="px-6 py-3 font-semibold">Fecha</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-flit-border-soft">
          {items.map((item) => (
            <tr key={item.id} className="hover:bg-flit-bg-modal/50">
              <td className="px-6 py-4 font-medium text-flit-text-primary">{item.reference}</td>
              <td className="px-6 py-4 text-flit-text-primary">{item.procedureTypeName}</td>
              <td className="px-6 py-4 text-flit-text-primary">{item.otDisplayName}</td>
              <td className="px-6 py-4 text-flit-text-primary">{item.vehicleQueryValue}</td>
              <td className="px-6 py-4">
                <StatusChip label={tramiteStatusLabel(item.status)} variant="warning" />
              </td>
              <td className="px-6 py-4 text-flit-text-secondary">{formatDate(item.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
