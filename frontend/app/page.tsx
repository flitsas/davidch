import Link from "next/link";
import { Can } from "@/components/auth/Can";
import { SessionRevokedBanner } from "@/components/auth/SessionRevokedBanner";
import { LogoutButton } from "@/components/auth/LogoutButton";
import { getSessionOrRedirect } from "@/lib/auth/session";

export default async function HomePage({
  searchParams,
}: {
  searchParams: Promise<{ revoked?: string }>;
}) {
  const session = await getSessionOrRedirect();

  const params = await searchParams;

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-6 p-8">
      {params.revoked === "1" && <SessionRevokedBanner />}
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">FLIT Identidad</h1>
        <div className="flex items-center gap-4 text-sm text-zinc-600">
          <span>{session.email}</span>
          <LogoutButton />
        </div>
      </header>
      <p className="text-zinc-700">
        Sesión activa. Roles: {session.roles.join(", ") || "—"}
      </p>
      <div className="flex flex-wrap gap-3">
        <Link
          href="/admin/users"
          className="rounded border border-zinc-300 px-4 py-2 text-sm hover:bg-zinc-50"
        >
          Administración
        </Link>
        <Can permission="generar_consolidado" session={session}>
          <button
            type="button"
            className="rounded bg-emerald-700 px-4 py-2 text-sm text-white"
          >
            Generar consolidado
          </button>
        </Can>
      </div>
    </main>
  );
}
