import { notFound } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import type { OtDetail, OtTab } from "@/lib/admin/ot-types";
import { parseOtTab } from "@/lib/admin/ot-types";
import { otStatusLabel } from "@/lib/admin/ot-api";
import { OtTabs } from "@/components/admin/OtTabs";
import { DocumentosTab } from "@/components/admin/ot-tabs/DocumentosTab";
import { IntegracionTab } from "@/components/admin/ot-tabs/IntegracionTab";
import { PerfilTab } from "@/components/admin/ot-tabs/PerfilTab";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { StatusChip } from "@/components/flit/Chip";

function TabPanel({ ot, tab }: { ot: OtDetail; tab: OtTab }) {
  switch (tab) {
    case "perfil":
      return <PerfilTab ot={ot} />;
    case "integracion":
      return <IntegracionTab otId={ot.id} apiBase="admin" />;
    case "documentos":
      return <DocumentosTab otId={ot.id} apiBase="admin" />;
  }
}

export default async function OtDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ tab?: string }>;
}) {
  const { id } = await params;
  const { tab: tabParam } = await searchParams;
  const activeTab = parseOtTab(tabParam);

  const res = await apiFetch(`/api/v1/admin/ot/${id}`);
  if (res.status === 404) {
    notFound();
  }

  if (!res.ok) {
    return (
      <FlitCard>
        <p className="text-sm text-flit-danger" role="alert">
          No se pudo cargar el organismo de tránsito.
        </p>
        <FlitLink href="/admin/ot" className="mt-4 inline-block">
          Volver al listado
        </FlitLink>
      </FlitCard>
    );
  }

  const ot = (await res.json()) as OtDetail;

  return (
    <>
      <PageHeaderCard
        title={ot.displayName}
        subtitle={`DIVIPOL ${ot.divipolCode} · Tenant ${ot.tenant.slug}`}
      />

      <p className="mb-4 flex flex-wrap items-center gap-3">
        <FlitLink href="/admin/ot">← Volver al listado</FlitLink>
        <StatusChip
          label={otStatusLabel(ot.status)}
          variant={ot.status === 1 ? "danger" : "success"}
        />
      </p>

      <FlitCard>
        <OtTabs otId={ot.id} activeTab={activeTab} />
        <div className="pt-6">
          <TabPanel ot={ot} tab={activeTab} />
        </div>
      </FlitCard>
    </>
  );
}
