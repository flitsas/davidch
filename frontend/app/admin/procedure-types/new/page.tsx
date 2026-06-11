import { PageHeaderCard, FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default function NewProcedureTypePlaceholderPage() {
  return (
    <>
      <PageHeaderCard
        title="Nuevo tipo de trámite"
        subtitle="Asistente de parametrización — disponible en la siguiente entrega"
      />
      <FlitCard>
        <p className="text-sm text-flit-text-secondary">
          El wizard de creación se implementa en la historia #10005.
        </p>
        <FlitLink href="/admin/procedure-types" className="mt-4 inline-block">
          Volver al listado
        </FlitLink>
      </FlitCard>
    </>
  );
}
