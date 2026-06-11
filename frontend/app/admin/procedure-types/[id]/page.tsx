import Link from "next/link";

export default function EditProcedureTypePlaceholderPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  return (
    <FlitCardWrapper params={params} />
  );
}

async function FlitCardWrapper({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold text-flit-text-brand">Editar tipo de trámite</h1>
      <p className="text-sm text-flit-text-secondary">
        Wizard de edición (ID: {id}) — disponible en la historia #10005.
      </p>
      <Link href="/admin/procedure-types" className="text-flit-blue hover:underline">
        Volver al listado
      </Link>
    </div>
  );
}
