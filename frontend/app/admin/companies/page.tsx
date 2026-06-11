import { apiFetch } from "@/lib/auth/api-client";
import { buildCompaniesIndexQuery, companyStatusLabel } from "@/lib/admin/companies-api";
import type {
  CompaniesIndexSearchParams,
  CompanyIndexResponse,
} from "@/lib/admin/companies-types";
import { CompaniesIndexFilters } from "@/components/admin/CompaniesIndexFilters";
import { CompaniesPagination } from "@/components/admin/CompaniesPagination";
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

export default async function AdminCompaniesPage({
  searchParams,
}: {
  searchParams: Promise<CompaniesIndexSearchParams>;
}) {
  const params = await searchParams;
  const query = buildCompaniesIndexQuery(params);
  const res = await apiFetch(`/api/v1/admin/companies/index?${query}`);

  const data: CompanyIndexResponse = res.ok
    ? await res.json()
    : { items: [], totalCount: 0, page: 1, pageSize: 20 };

  return (
    <>
      <PageHeaderCard
        title="Compañías B2B"
        subtitle="Consola de gobierno multi-tenant — indexación y filtros"
        actions={
          <FlitLink
            href="/admin/companies/new"
            className="flit-gradient-primary inline-flex min-h-11 items-center rounded-flit-pill px-6 text-sm font-semibold text-flit-text-inverse shadow-flit-button"
          >
            Nueva compañía
          </FlitLink>
        }
      />

      <FlitCard className="mb-6">
        <h2 className="mb-4 text-sm font-semibold text-flit-text-brand">Filtros</h2>
        <CompaniesIndexFilters initial={params} />
      </FlitCard>

      <FlitCard className="overflow-hidden p-0">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Listado</h2>
        </div>

        {!res.ok && (
          <p className="px-6 py-8 text-sm text-flit-danger" role="alert">
            No se pudo cargar el listado de compañías.
          </p>
        )}

        {res.ok && data.items.length === 0 && (
          <p className="px-6 py-8 text-sm text-flit-text-secondary">
            No hay compañías que coincidan con los filtros.
          </p>
        )}

        {res.ok && data.items.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="bg-flit-bg-table-header text-flit-text-secondary">
                <tr>
                  <th className="px-6 py-3 font-semibold">NIT</th>
                  <th className="px-6 py-3 font-semibold">Razón social</th>
                  <th className="px-6 py-3 font-semibold">Estado</th>
                  <th className="px-6 py-3 font-semibold">Creada</th>
                  <th className="px-6 py-3 font-semibold">Actualizada</th>
                  <th className="px-6 py-3 font-semibold">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-flit-border-soft">
                {data.items.map((company) => (
                  <tr key={company.id} className="hover:bg-flit-bg-modal/50">
                    <td className="px-6 py-4 font-medium text-flit-text-primary">
                      <Link
                        href={`/admin/companies/${company.id}`}
                        className="text-flit-blue hover:underline"
                      >
                        {company.nit}
                      </Link>
                    </td>
                    <td className="px-6 py-4 text-flit-text-primary">
                      {company.legalName}
                    </td>
                    <td className="px-6 py-4">
                      <StatusChip
                        label={companyStatusLabel(company.status)}
                        variant={statusVariant(company.status)}
                      />
                    </td>
                    <td className="px-6 py-4 text-flit-text-secondary">
                      {formatDate(company.createdAt)}
                    </td>
                    <td className="px-6 py-4 text-flit-text-secondary">
                      {formatDate(company.updatedAt)}
                    </td>
                    <td className="px-6 py-4">
                      <FlitLink href={`/admin/companies/${company.id}`}>
                        Configurar
                      </FlitLink>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {res.ok && (
          <CompaniesPagination
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
