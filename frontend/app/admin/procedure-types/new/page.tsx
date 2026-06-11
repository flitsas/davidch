import { PageHeaderCard } from "@/components/flit/Card";
import { ProcedureTypeWizard } from "@/components/admin/ProcedureTypeWizard";

export default function NewProcedureTypePage() {
  return (
    <>
      <PageHeaderCard
        title="Nuevo tipo de trámite"
        subtitle="Asistente de parametrización — 4 pasos"
      />
      <ProcedureTypeWizard mode="create" />
    </>
  );
}
