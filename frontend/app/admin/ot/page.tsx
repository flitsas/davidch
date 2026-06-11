import { apiFetch } from "@/lib/auth/api-client";
import { buildOtIndexQuery, otStatusLabel } from "@/lib/admin/ot-api";
import type { OtIndexResponse, OtIndexSearchParams } from "@/lib/admin/ot-types";
import { OtIndexFilters } from "@/components/admin/OtIndexFilters";
import { OtPagination } from "@/components/admin/OtPagination";
import Link from "next/link";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { StatusChip } from "@/components/flit/Chip";

function statusVariant(status: number): "success" | "danger" {
  return status === 1 ? "danger" : "success";
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString("es-CO", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export default async function AdminOtPage({
  searchParams,
}: {
  searchParams: Promise<OtIndexSearchParams>;
}) {
  const params = await searchParams;
  const query = buildOtIndexQuery(params);
  const res = await apiFetch(`/api/v1/admin/ot/index?${query}`);

  const data: OtIndexResponse = res.ok
    ? await res.json()
    : { items: [], totalCount: 0, page: 1, pageSize: 20 };

  return (
    <>
      <PageHeaderCard
        title="Organismos de tránsito"
        subtitle="Consola SuperAdmin — indexación y aprovisionamiento OT"
        actions={
          <FlitLink
            href="/admin/ot/new"
            className="flit-gradient-primary inline-flex min-h-11 items-center rounded-flit-pill px-6 text-sm font-semibold text-flit-text-inverse shadow-flit-button"
          >
            Nuevo OT
          </FlitLink>
        }
      />

      <FlitCard className="mb-6">
        <h2 className="mb-4 text-sm font-semibold text-flit-text-brand">Filtros</h2>
        <OtIndexFilters initial={params} />
      </FlitCard>

      <FlitCard className="overflow-hidden p-0">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Listado</h2>
        </div>

        {!res.ok && (
          <p className="px-6 py-8 text-sm text-flit-danger" role="alert">
            No se pudo cargar el listado de organismos de tránsito.
          </p>
        )}

        {res.ok && data.items.length === 0 && (
          <p className="px-6 py-8 text-sm text-flit-text-secondary">
            No hay OT que coincidan con los filtros.
          </p>
        )}

        {res.ok && data.items.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="bg-flit-bg-table-header text-flit-text-secondary">
                <tr>
                  <th className="px-6 py-3 font-semibold">DIVIPOL</th>
                  <th className="px-6 py-3 font-semibold">Nombre</th>
                  <th className="px-6 py-3 font-semibold">Estado</th>
                  <th className="px-6 py-3 font-semibold">Actualizado</th>
                  <th className="px-6 py-3 font-semibold">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-flit-border-soft">
                {data.items.map((ot) => (
                  <tr key={ot.id} className="hover:bg-flit-bg-modal/50">
                    <td className="px-6 py-4 font-medium text-flit-text-primary">
                      <Link
                        href={`/admin/ot/${ot.id}`}
                        className="text-flit-blue hover:underline"
                      >
                        {ot.divipolCode}
                      </Link>
                    </td>
                    <td className="px-6 py-4 text-flit-text-primary">{ot.displayName}</td>
                    <td className="px-6 py-4">
                      <StatusChip
                        label={otStatusLabel(ot.status)}
                        variant={statusVariant(ot.status)}
                      />
                    </td>
                    <td className="px-6 py-4 text-flit-text-secondary">
                      {formatDate(ot.updatedAt)}
                    </td>
                    <td className="px-6 py-4">
                      <FlitLink href={`/admin/ot/${ot.id}`}>Configurar</FlitLink>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {res.ok && (
          <OtPagination
            page={data.page}
            pageSize={data.pageSize}
            totalCount={data.totalCount}
            searchParams={params}
          />
        )}
      </FlitCard>
    </>
  );
}
