import { IntegracionTab } from "@/components/admin/ot-tabs/IntegracionTab";
import { PageHeaderCard } from "@/components/flit/Card";
import { FlitCard } from "@/components/flit/Card";

export default function OtSettingsIntegracionPage() {
  return (
    <>
      <PageHeaderCard
        title="Configuración OT"
        subtitle="Modo de integración con Dashboard FLIT o Quipux (QX)"
      />

      <FlitCard>
        <IntegracionTab apiBase="settings" />
      </FlitCard>
    </>
  );
}
