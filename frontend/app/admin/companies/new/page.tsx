import { apiFetch } from "@/lib/auth/api-client";
import type { TenantSummary } from "@/lib/admin/types";
import { CompanyCreateForm } from "@/components/admin/CompanyCreateForm";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default async function NewCompanyPage() {
  const tenantsRes = await apiFetch("/api/tenants");
  const tenants: TenantSummary[] = tenantsRes.ok ? await tenantsRes.json() : [];

  return (
    <>
      <PageHeaderCard
        title="Nueva compañía B2B"
        subtitle="Crear tenant nuevo o vincular uno existente"
      />

      <p className="mb-4">
        <FlitLink href="/admin/companies">← Volver al listado</FlitLink>
      </p>

      <FlitCard>
        {tenants.length === 0 && (
          <p className="mb-4 text-sm text-flit-text-secondary">
            No hay tenants disponibles para vincular. Usa el modo «Crear nuevo tenant».
          </p>
        )}
        <CompanyCreateForm tenants={tenants} />
      </FlitCard>
    </>
  );
}
