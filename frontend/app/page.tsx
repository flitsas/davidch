import { Can } from "@/components/auth/Can";
import { SessionRevokedBanner } from "@/components/auth/SessionRevokedBanner";
import { LogoutButton } from "@/components/auth/LogoutButton";
import { AppShell } from "@/components/flit/AppShell";
import { FlitCard, PageHeaderCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { GradientButton } from "@/components/flit/Button";
import { buildAdminNav } from "@/lib/flit/nav";
import { getSessionOrRedirect } from "@/lib/auth/session";

export default async function HomePage({
  searchParams,
}: {
  searchParams: Promise<{ revoked?: string }>;
}) {
  const session = await getSessionOrRedirect();
  const params = await searchParams;
  const hasAdminNav = buildAdminNav(session).length > 1;

  return (
    <AppShell email={session.email} nav={buildAdminNav(session)} headerActions={<LogoutButton />}>
      {params.revoked === "1" && <SessionRevokedBanner />}
      <PageHeaderCard title="FLIT Identidad" subtitle="Sesión activa y permisos cargados" />
      <FlitCard>
        <dl className="grid gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-sm font-semibold text-flit-text-secondary">Correo</dt>
            <dd className="mt-1 text-base text-flit-text-primary">{session.email}</dd>
          </div>
          <div>
            <dt className="text-sm font-semibold text-flit-text-secondary">Roles</dt>
            <dd className="mt-1 text-base text-flit-text-primary">
              {session.roles.join(", ") || "—"}
            </dd>
          </div>
        </dl>
        <div className="mt-8 flex flex-wrap gap-3">
          {hasAdminNav && (
            <FlitLink
              href="/admin/users"
              className="inline-flex min-h-11 items-center rounded-flit-pill border border-flit-border-soft bg-flit-bg-card px-6 text-sm font-semibold no-underline hover:bg-flit-bg-modal"
            >
              Administración
            </FlitLink>
          )}
          <Can permission="generar_consolidado" session={session}>
            <GradientButton className="min-h-11 px-6 text-sm">Generar consolidado</GradientButton>
          </Can>
        </div>
      </FlitCard>
    </AppShell>
  );
}
