import Link from "next/link";
import { redirect } from "next/navigation";
import { LogoutButton } from "@/components/auth/LogoutButton";
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
      <main className="mx-auto max-w-3xl p-8">
        <p>No tienes permisos para acceder a la administración.</p>
        <Link href="/" className="underline">
          Volver al inicio
        </Link>
      </main>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50">
      <header className="border-b border-zinc-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center gap-6 px-6 py-4">
          <Link href="/" className="font-semibold">
            FLIT
          </Link>
          <nav className="flex gap-4 text-sm">
            {canUsers && (
              <Link href="/admin/users" className="hover:underline">
                Usuarios
              </Link>
            )}
            {canRoles && (
              <Link href="/admin/roles" className="hover:underline">
                Roles
              </Link>
            )}
          </nav>
          <span className="ml-auto flex items-center gap-4 text-sm text-zinc-600">
            {session.email}
            <LogoutButton />
          </span>
        </div>
      </header>
      <main className="mx-auto max-w-5xl p-6">{children}</main>
    </div>
  );
}
