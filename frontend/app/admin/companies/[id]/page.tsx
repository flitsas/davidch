import { notFound } from "next/navigation";
import { apiFetch } from "@/lib/auth/api-client";
import type { CompanyDetail, CompanyTab } from "@/lib/admin/companies-types";
import { parseCompanyTab } from "@/lib/admin/companies-types";
import { companyStatusLabel } from "@/lib/admin/companies-api";
import { CompanyTabs } from "@/components/admin/CompanyTabs";
import { CompanyProfileForm } from "@/components/admin/CompanyProfileForm";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { StatusChip } from "@/components/flit/Chip";

function TabPanel({ tab }: { tab: CompanyTab }) {
  return (
    <p className="text-sm text-flit-text-secondary">
      Sección «{tab}» — contenido en entregas posteriores.
    </p>
  );
}

export default async function CompanyDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ tab?: string }>;
}) {
  const { id } = await params;
  const { tab: tabParam } = await searchParams;
  const activeTab = parseCompanyTab(tabParam);

  const res = await apiFetch(`/api/v1/admin/companies/${id}`);
  if (res.status === 404) {
    notFound();
  }

  if (!res.ok) {
    return (
      <FlitCard>
        <p className="text-sm text-flit-danger" role="alert">
          No se pudo cargar la compañía.
        </p>
        <FlitLink href="/admin/companies" className="mt-4 inline-block">
          Volver al listado
        </FlitLink>
      </FlitCard>
    );
  }

  const company = (await res.json()) as CompanyDetail;

  return (
    <>
      <PageHeaderCard
        title={company.legalName}
        subtitle={`NIT ${company.nit} · Tenant ${company.tenant.slug}`}
      />

      <p className="mb-4 flex flex-wrap items-center gap-3">
        <FlitLink href="/admin/companies">← Volver al listado</FlitLink>
        <StatusChip
          label={companyStatusLabel(company.status)}
          variant={company.status === 1 ? "danger" : "success"}
        />
      </p>

      <FlitCard className="mb-6">
        <h2 className="mb-4 text-sm font-semibold text-flit-text-brand">
          Datos generales
        </h2>
        <CompanyProfileForm company={company} />
      </FlitCard>

      <FlitCard>
        <CompanyTabs companyId={company.id} activeTab={activeTab} />
        <div className="pt-6">
          <TabPanel tab={activeTab} />
        </div>
      </FlitCard>
    </>
  );
}
