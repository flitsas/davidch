"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { GradientButton } from "@/components/flit/Button";
import { ProcedureInstanceWizard } from "@/components/tramites/ProcedureInstanceWizard";

export function TramitesPageActions({ canCreate }: { canCreate: boolean }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);

  if (!canCreate) return null;

  return (
    <>
      <GradientButton
        type="button"
        className="min-h-11 px-6 text-sm"
        data-testid="tramites-new"
        onClick={() => setOpen(true)}
      >
        Nuevo trámite
      </GradientButton>
      {open && (
        <ProcedureInstanceWizard
          onClose={() => setOpen(false)}
          onSuccess={() => {
            setOpen(false);
            router.refresh();
          }}
        />
      )}
    </>
  );
}
