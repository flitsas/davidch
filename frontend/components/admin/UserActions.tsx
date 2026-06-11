"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { FlitButton, DangerButton } from "@/components/flit/Button";

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
    <div className="flex flex-wrap items-center gap-2">
      <FlitButton
        variant="ghost"
        onClick={forceReset}
        disabled={loading !== null}
        className="min-h-9 px-4 text-xs"
      >
        {loading === "reset" ? "Enviando…" : "Forzar reset"}
      </FlitButton>
      <DangerButton
        onClick={blockUser}
        disabled={loading !== null}
        className="min-h-9 px-4 text-xs"
      >
        {loading === "block" ? "Bloqueando…" : "Bloquear"}
      </DangerButton>
      {message && (
        <span className="text-xs text-flit-text-secondary" aria-live="polite">
          {message}
        </span>
      )}
    </div>
  );
}
