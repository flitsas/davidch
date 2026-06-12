import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";

export default async function TramitesDashboardLayout({ children }: { children: React.ReactNode }) {
  const session = await getSessionOrRedirect();
  const canAccess = hasPermission(session, "users:read");

  if (!canAccess) {
    return (
      <FlitCard>
        <h1 className="text-xl font-bold text-flit-text-brand">Acceso denegado</h1>
        <p className="mt-2 text-sm text-flit-text-secondary">
          El dashboard de trámites requiere el permiso users:read o rol SuperAdmin.
        </p>
        <FlitLink href="/" className="mt-4 inline-block">
          Volver al inicio
        </FlitLink>
      </FlitCard>
    );
  }

  return children;
}
