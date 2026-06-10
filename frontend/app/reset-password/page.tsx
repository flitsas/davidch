"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";

function ResetForm() {
  const router = useRouter();
  const params = useSearchParams();
  const token = params.get("token") ?? "";
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (password !== confirm) {
      setError("Las contraseñas no coinciden");
      return;
    }
    setError(null);
    setLoading(true);
    try {
      const res = await fetch("/api/auth/reset-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, new_password: password }),
        credentials: "include",
      });
      if (!res.ok) {
        setError("Enlace inválido o expirado");
        return;
      }
      router.push("/login");
    } finally {
      setLoading(false);
    }
  }

  if (!token) {
    return (
      <main className="mx-auto max-w-md p-8">
        <p className="text-red-600">Enlace de restablecimiento inválido.</p>
      </main>
    );
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center p-8">
      <form onSubmit={onSubmit} className="space-y-4 rounded-lg border border-zinc-200 p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Nueva contraseña</h1>
        <input
          className="w-full rounded border border-zinc-300 px-3 py-2"
          type="password"
          placeholder="Nueva contraseña"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
        />
        <input
          className="w-full rounded border border-zinc-300 px-3 py-2"
          type="password"
          placeholder="Confirmar contraseña"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          required
          minLength={8}
        />
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full rounded bg-zinc-900 px-4 py-2 text-white disabled:opacity-50"
        >
          {loading ? "Guardando…" : "Restablecer contraseña"}
        </button>
        <p className="text-center text-sm">
          <Link href="/login" className="underline">
            Ir a iniciar sesión
          </Link>
        </p>
      </form>
    </main>
  );
}

export default function ResetPasswordPage() {
  return (
    <Suspense>
      <ResetForm />
    </Suspense>
  );
}
