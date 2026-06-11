"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { FlitCard } from "@/components/flit/Card";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";

export function CreateRoleForm() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await fetch("/api/roles", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
        credentials: "include",
      });
      if (!res.ok) {
        setError("No se pudo crear el rol");
        return;
      }
      setName("");
      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <FlitCard>
      <form onSubmit={onSubmit} className="flex flex-wrap items-end gap-4">
        <div className="min-w-[240px] flex-1">
          <FlitInput
            label="Nuevo rol"
            name="name"
            id="role-name"
            placeholder="Nombre del rol…"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoComplete="off"
            error={error}
          />
        </div>
        <GradientButton type="submit" disabled={loading} className="min-h-10 shrink-0 px-6 text-sm">
          {loading ? "Creando…" : "Crear rol"}
        </GradientButton>
      </form>
    </FlitCard>
  );
}
