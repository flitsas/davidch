import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";

export default async function OtSettingsLayout({ children }: { children: React.ReactNode }) {
  const session = await getSessionOrRedirect();

  if (session.isSuperAdmin || !hasPermission(session, "tramites:update")) {
    return (
      <FlitCard>
        <h1 className="text-xl font-bold text-flit-text-brand">Acceso denegado</h1>
        <p className="mt-2 text-sm text-flit-text-secondary">
          La configuración OT requiere el permiso tramites:update en su tenant.
        </p>
        <FlitLink href="/" className="mt-4 inline-block">
          Volver al inicio
        </FlitLink>
      </FlitCard>
    );
  }

  return children;
}
