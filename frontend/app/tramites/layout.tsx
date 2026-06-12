import { LogoutButton } from "@/components/auth/LogoutButton";
import { AppShell } from "@/components/flit/AppShell";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { buildAdminNav } from "@/lib/flit/nav";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";

export default async function TramitesLayout({ children }: { children: React.ReactNode }) {
  const session = await getSessionOrRedirect();

  if (!hasPermission(session, "tramites:read")) {
    return (
      <AppShell email={session.email} nav={buildAdminNav(session)} headerActions={<LogoutButton />}>
        <FlitCard>
          <h1 className="text-xl font-bold text-flit-text-brand">Acceso denegado</h1>
          <p className="mt-2 text-sm text-flit-text-secondary">
            La consulta de trámites requiere el permiso tramites:read.
          </p>
          <FlitLink href="/" className="mt-4 inline-block">
            Volver al inicio
          </FlitLink>
        </FlitCard>
      </AppShell>
    );
  }

  return (
    <AppShell email={session.email} nav={buildAdminNav(session)} headerActions={<LogoutButton />}>
      {children}
    </AppShell>
  );
}
