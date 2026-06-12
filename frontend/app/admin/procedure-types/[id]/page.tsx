import { apiFetch } from "@/lib/auth/api-client";
import type { ProcedureTypeDetail } from "@/lib/admin/procedure-types-types";
import { PageHeaderCard } from "@/components/flit/Card";
import { ProcedureTypeWizard } from "@/components/admin/ProcedureTypeWizard";
import { FlitLink } from "@/components/flit/Link";

export default async function EditProcedureTypePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const res = await apiFetch(`/api/v1/admin/procedure-types/${id}`);

  if (!res.ok) {
    return (
      <>
        <PageHeaderCard title="Tipo de trámite no encontrado" />
        <FlitLink href="/admin/procedure-types">Volver al listado</FlitLink>
      </>
    );
  }

  const detail = (await res.json()) as ProcedureTypeDetail;

  return (
    <>
      <PageHeaderCard title={`Editar: ${detail.name}`} subtitle={`Código ${detail.code}`} />
      <ProcedureTypeWizard mode="edit" procedureTypeId={id} initial={detail} />
    </>
  );
}
