"use client";

import { useState } from "react";
import Link from "next/link";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await fetch("/api/auth/forgot-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
        credentials: "include",
      });
      setSent(true);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center p-8">
      <form onSubmit={onSubmit} className="space-y-4 rounded-lg border border-zinc-200 p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Recuperar contraseña</h1>
        {sent ? (
          <p className="text-sm text-zinc-700">
            Si el correo existe en el sistema, recibirás un enlace para restablecer tu contraseña.
          </p>
        ) : (
          <>
            <input
              className="w-full rounded border border-zinc-300 px-3 py-2"
              type="email"
              placeholder="Correo electrónico"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
            <button
              type="submit"
              disabled={loading}
              className="w-full rounded bg-zinc-900 px-4 py-2 text-white disabled:opacity-50"
            >
              {loading ? "Enviando…" : "Enviar enlace"}
            </button>
          </>
        )}
        <p className="text-center text-sm">
          <Link href="/login" className="underline">
            Volver al inicio de sesión
          </Link>
        </p>
      </form>
    </main>
  );
}
