import { apiFetch } from "@/lib/auth/api-client";
import {
  buildProcedureTypeIndexQuery,
  procedureTypeStatusLabel,
  vehicleQueryModeLabel,
} from "@/lib/admin/procedure-types-api";
import type {
  ProcedureTypeIndexResponse,
  ProcedureTypeIndexSearchParams,
} from "@/lib/admin/procedure-types-types";
import { ProcedureTypeStatusButton } from "@/components/admin/ProcedureTypeStatusButton";
import { PageHeaderCard, FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { StatusChip } from "@/components/flit/Chip";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("es-CO", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export default async function AdminProcedureTypesPage({
  searchParams,
}: {
  searchParams: Promise<ProcedureTypeIndexSearchParams>;
}) {
  const params = await searchParams;
  const query = buildProcedureTypeIndexQuery(params);
  const res = await apiFetch(`/api/v1/admin/procedure-types/index?${query}`);

  const data: ProcedureTypeIndexResponse = res.ok
    ? await res.json()
    : { items: [], totalCount: 0, page: 1, pageSize: 20 };

  return (
    <>
      <PageHeaderCard
        title="Tipos de trámite"
        subtitle="Parametrizador SuperAdmin — definición de trámites dinámicos"
        actions={
          <FlitLink
            href="/admin/procedure-types/new"
            className="flit-gradient-primary inline-flex min-h-11 items-center rounded-flit-pill px-6 text-sm font-semibold text-flit-text-inverse shadow-flit-button"
            data-testid="procedure-type-new"
          >
            Nuevo tipo de trámite
          </FlitLink>
        }
      />

      <FlitCard className="overflow-hidden p-0" data-testid="procedure-types-index">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Listado</h2>
        </div>

        {!res.ok && (
          <p className="px-6 py-8 text-sm text-flit-danger" role="alert">
            No se pudo cargar el listado de tipos de trámite.
          </p>
        )}

        {res.ok && data.items.length === 0 && (
          <p className="px-6 py-8 text-sm text-flit-text-secondary">
            No hay tipos de trámite registrados.
          </p>
        )}

        {res.ok && data.items.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="bg-flit-bg-table-header text-flit-text-secondary">
                <tr>
                  <th className="px-6 py-3 font-semibold">Nombre</th>
                  <th className="px-6 py-3 font-semibold">Código</th>
                  <th className="px-6 py-3 font-semibold">Vehículo</th>
                  <th className="px-6 py-3 font-semibold">Actores</th>
                  <th className="px-6 py-3 font-semibold">Documentos</th>
                  <th className="px-6 py-3 font-semibold">Estado</th>
                  <th className="px-6 py-3 font-semibold">Actualizado</th>
                  <th className="px-6 py-3 font-semibold">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-flit-border-soft">
                {data.items.map((item) => (
                  <tr key={item.id} className="hover:bg-flit-bg-modal/50">
                    <td className="px-6 py-4 font-medium text-flit-text-primary">{item.name}</td>
                    <td className="px-6 py-4 text-flit-text-secondary">{item.code}</td>
                    <td className="px-6 py-4 text-flit-text-primary">
                      {vehicleQueryModeLabel(item.vehicleQueryMode)}
                    </td>
                    <td className="px-6 py-4 text-flit-text-primary">{item.actorCount}</td>
                    <td className="px-6 py-4 text-flit-text-primary">{item.documentCount}</td>
                    <td className="px-6 py-4">
                      <StatusChip
                        label={procedureTypeStatusLabel(item.isActive)}
                        variant={item.isActive ? "success" : "danger"}
                      />
                    </td>
                    <td className="px-6 py-4 text-flit-text-secondary">
                      {formatDate(item.updatedAt)}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap items-start gap-3">
                        <FlitLink href={`/admin/procedure-types/${item.id}`}>Editar</FlitLink>
                        <ProcedureTypeStatusButton id={item.id} isActive={item.isActive} />
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </FlitCard>
    </>
  );
}
