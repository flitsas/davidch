import { DocumentosTab } from "@/components/admin/ot-tabs/DocumentosTab";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";

export default function OtSettingsDocumentosPage() {
  return (
    <>
      <PageHeaderCard
        title="Orden de documentos"
        subtitle="Precedencia de documentos por tipo de trámite (RF09 / RF10)"
      />

      <p className="mb-4">
        <FlitLink href="/ot/settings/integracion">← Integración</FlitLink>
      </p>

      <FlitCard>
        <DocumentosTab apiBase="settings" />
      </FlitCard>
    </>
  );
}
