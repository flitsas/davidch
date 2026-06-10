"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

export function UserActions({ userId, userEmail }: { userId: string; userEmail: string }) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState<string | null>(null);

  async function forceReset() {
    if (!confirm(`¿Forzar reset de contraseña para ${userEmail}?`)) return;
    setLoading("reset");
    setMessage(null);
    const res = await fetch(`/api/users/${userId}/force-reset`, {
      method: "POST",
      credentials: "include",
    });
    setLoading(null);
    if (!res.ok) {
      setMessage("No se pudo iniciar el reset forzado");
      return;
    }
    setMessage("Correo de reset enviado");
    router.refresh();
  }

  async function blockUser() {
    if (!confirm(`¿Bloquear a ${userEmail}? No podrá iniciar sesión.`)) return;
    setLoading("block");
    setMessage(null);
    const res = await fetch(`/api/users/${userId}/block`, {
      method: "POST",
      credentials: "include",
    });
    setLoading(null);
    if (!res.ok) {
      setMessage("No se pudo bloquear el usuario");
      return;
    }
    setMessage("Usuario bloqueado");
    router.refresh();
  }

  return (
    <div className="mt-2 flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={forceReset}
        disabled={loading !== null}
        className="rounded border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-50 disabled:opacity-50"
      >
        {loading === "reset" ? "…" : "Forzar reset"}
      </button>
      <button
        type="button"
        onClick={blockUser}
        disabled={loading !== null}
        className="rounded border border-red-300 px-2 py-1 text-xs text-red-700 hover:bg-red-50 disabled:opacity-50"
      >
        {loading === "block" ? "…" : "Bloquear"}
      </button>
      {message && <span className="text-xs text-zinc-600">{message}</span>}
    </div>
  );
}
