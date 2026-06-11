import { apiFetch } from "@/lib/auth/api-client";
import type { TenantSummary } from "@/lib/admin/types";
import { OtCreateForm } from "@/components/admin/OtCreateForm";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default async function NewOtPage() {
  const tenantsRes = await apiFetch("/api/tenants");
  const tenants: TenantSummary[] = tenantsRes.ok ? await tenantsRes.json() : [];

  return (
    <>
      <PageHeaderCard
        title="Nuevo organismo de tránsito"
        subtitle="Crear tenant OT nuevo o vincular uno existente"
      />

      <p className="mb-4">
        <FlitLink href="/admin/ot">← Volver al listado</FlitLink>
      </p>

      <FlitCard>
        {tenants.length === 0 && (
          <p className="mb-4 text-sm text-flit-text-secondary">
            No hay tenants disponibles para vincular. Usa el modo «Crear nuevo tenant OT».
          </p>
        )}
        <OtCreateForm tenants={tenants} />
      </FlitCard>
    </>
  );
}
