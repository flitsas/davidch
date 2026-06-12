import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";

export default async function TramitesIndexLayout({ children }: { children: React.ReactNode }) {
  const session = await getSessionOrRedirect();

  if (!hasPermission(session, "tramites:read")) {
    return (
      <FlitCard>
        <h1 className="text-xl font-bold text-flit-text-brand">Acceso denegado</h1>
        <p className="mt-2 text-sm text-flit-text-secondary">
          La consulta de trámites requiere el permiso tramites:read.
        </p>
        <FlitLink href="/" className="mt-4 inline-block">
          Volver al inicio
        </FlitLink>
      </FlitCard>
    );
  }

  return children;
}
