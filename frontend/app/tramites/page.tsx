import { TramitesPageActions } from "@/components/tramites/TramitesPageActions";
import { TramitesIndexTable } from "@/components/tramites/TramitesIndexTable";
import { PageHeaderCard, FlitCard } from "@/components/flit/Card";
import { apiFetch } from "@/lib/auth/api-client";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";
import { buildTramitesIndexQuery } from "@/lib/tramites/api";
import type { TramitesIndexResponse, TramitesIndexSearchParams } from "@/lib/tramites/types";

export default async function TramitesPage({
  searchParams,
}: {
  searchParams: Promise<TramitesIndexSearchParams>;
}) {
  const session = await getSessionOrRedirect();
  const canCreate = hasPermission(session, "tramites:create");
  const params = await searchParams;
  const query = buildTramitesIndexQuery(params);
  const res = await apiFetch(`/api/v1/tramites/index?${query}`);

  const data: TramitesIndexResponse = res.ok
    ? await res.json()
    : { items: [], totalCount: 0, page: 1, pageSize: 20 };

  return (
    <>
      <PageHeaderCard
        title="Trámites"
        subtitle="Instancias de trámite del tenant — más recientes primero"
        actions={<TramitesPageActions canCreate={canCreate} />}
      />

      <FlitCard className="overflow-hidden p-0" data-testid="tramites-index">
        <div className="border-b border-flit-border-soft bg-flit-bg-table-header px-6 py-4">
          <h2 className="text-sm font-semibold text-flit-text-brand">Listado</h2>
        </div>

        {!res.ok && (
          <p className="px-6 py-8 text-sm text-flit-danger" role="alert">
            No se pudo cargar el listado de trámites.
          </p>
        )}

        {res.ok && <TramitesIndexTable items={data.items} />}
      </FlitCard>
    </>
  );
}
