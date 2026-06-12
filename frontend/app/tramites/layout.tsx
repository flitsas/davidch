import { LogoutButton } from "@/components/auth/LogoutButton";
import { AppShell } from "@/components/flit/AppShell";
import { buildAdminNav } from "@/lib/flit/nav";
import { getSessionOrRedirect } from "@/lib/auth/session";

export default async function TramitesLayout({ children }: { children: React.ReactNode }) {
  const session = await getSessionOrRedirect();

  return (
    <AppShell email={session.email} nav={buildAdminNav(session)} headerActions={<LogoutButton />}>
      {children}
    </AppShell>
  );
}
