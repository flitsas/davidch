"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { FlitButton } from "@/components/flit/Button";

type Props = {
  id: string;
  isActive: boolean;
};

export function ProcedureTypeStatusButton({ id, isActive }: Props) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggleStatus() {
    setLoading(true);
    setError(null);

    try {
      const res = await fetch(`/api/v1/admin/procedure-types/${id}/status`, {
        method: "PATCH",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ isActive: !isActive }),
      });

      if (!res.ok) {
        const body = (await res.json().catch(() => null)) as { error?: string } | null;
        setError(body?.error ?? "No se pudo actualizar el estado.");
        return;
      }

      router.refresh();
    } catch {
      setError("No se pudo actualizar el estado.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col gap-1">
      <FlitButton
        type="button"
        variant={isActive ? "ghost" : "primary"}
        disabled={loading}
        onClick={toggleStatus}
        data-testid={`procedure-type-status-${id}`}
      >
        {loading ? "Guardando…" : isActive ? "Desactivar" : "Activar"}
      </FlitButton>
      {error ? (
        <span className="text-xs text-flit-danger" role="alert">
          {error}
        </span>
      ) : null}
    </div>
  );
}
