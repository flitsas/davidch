import { LogoutButton } from "@/components/auth/LogoutButton";
import { AppShell } from "@/components/flit/AppShell";
import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { buildAdminNav } from "@/lib/flit/nav";
import { getSessionOrRedirect, hasPermission } from "@/lib/auth/session";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSessionOrRedirect();

  const canUsers = hasPermission(session, "users:read");
  const canRoles = hasPermission(session, "roles:read");

  if (!canUsers && !canRoles) {
    return (
      <AppShell
        email={session.email}
        nav={buildAdminNav(session)}
        headerActions={<LogoutButton />}
      >
        <FlitCard>
          <p className="text-flit-text-secondary">
            No tienes permisos para acceder a la administración.
          </p>
          <FlitLink href="/" className="mt-4 inline-block">
            Volver al inicio
          </FlitLink>
        </FlitCard>
      </AppShell>
    );
  }

  return (
    <AppShell
      email={session.email}
      nav={buildAdminNav(session)}
      headerActions={<LogoutButton />}
    >
      {children}
    </AppShell>
  );
}
