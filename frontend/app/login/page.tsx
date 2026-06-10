"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { SessionRevokedBanner } from "@/components/auth/SessionRevokedBanner";

function LoginForm() {
  const router = useRouter();
  const params = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
        credentials: "include",
      });
      if (!res.ok) {
        setError("Credenciales inválidas");
        return;
      }
      router.push(params.get("reason") === "session_revoked" ? "/?revoked=1" : "/");
      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center p-8">
      {params.get("reason") === "session_revoked" && <SessionRevokedBanner />}
      <form onSubmit={onSubmit} className="space-y-4 rounded-lg border border-zinc-200 p-6 shadow-sm">
        <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
        <div>
          <label className="mb-1 block text-sm font-medium" htmlFor="email">
            Correo
          </label>
          <input
            id="email"
            className="w-full rounded border border-zinc-300 px-3 py-2"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium" htmlFor="password">
            Contraseña
          </label>
          <input
            id="password"
            className="w-full rounded border border-zinc-300 px-3 py-2"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full rounded bg-zinc-900 px-4 py-2 text-white hover:bg-zinc-800 disabled:opacity-50"
        >
          {loading ? "Entrando…" : "Entrar"}
        </button>
        <p className="text-center text-sm text-zinc-600">
          <Link href="/forgot-password" className="underline">
            ¿Olvidaste tu contraseña?
          </Link>
        </p>
      </form>
    </main>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
