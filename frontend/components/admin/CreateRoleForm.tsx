"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export function CreateRoleForm() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
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
  }

  return (
    <form onSubmit={onSubmit} className="flex gap-2">
      <input
        className="flex-1 rounded border border-zinc-300 px-3 py-2 text-sm"
        placeholder="Nombre del rol"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <button type="submit" className="rounded bg-zinc-900 px-3 py-2 text-sm text-white">
        Crear rol
      </button>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </form>
  );
}
