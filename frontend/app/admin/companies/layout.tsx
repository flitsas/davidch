import { FlitCard } from "@/components/flit/Card";
import { FlitLink } from "@/components/flit/Link";
import { getSessionOrRedirect } from "@/lib/auth/session";

export default async function AdminCompaniesLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSessionOrRedirect();

  if (!session.isSuperAdmin) {
    return (
      <FlitCard>
        <h1 className="text-xl font-bold text-flit-text-brand">Acceso denegado</h1>
        <p className="mt-2 text-sm text-flit-text-secondary">
          La consola de compañías está disponible solo para Super Administrador.
        </p>
        <FlitLink href="/login" className="mt-4 mr-4 inline-block">
          Iniciar sesión
        </FlitLink>
        <FlitLink href="/" className="mt-4 inline-block">
          Volver al inicio
        </FlitLink>
      </FlitCard>
    );
  }

  return children;
}
