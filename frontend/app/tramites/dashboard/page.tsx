import { TramitesDashboard } from "@/components/tramites/dashboard/TramitesDashboard";
import { PageHeaderCard } from "@/components/flit/Card";
import { getSessionOrRedirect } from "@/lib/auth/session";

export default async function TramitesDashboardPage() {
  const session = await getSessionOrRedirect();

  return (
    <>
      <PageHeaderCard
        title="Dashboard de Trámites"
        subtitle="Métricas operativas por tipo de trámite y productividad de radicadores"
      />
      <TramitesDashboard
        isSuperAdmin={session.isSuperAdmin}
        defaultTenantId={session.tenantId}
      />
    </>
  );
}
