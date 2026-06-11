"use client";

import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { SessionRevokedBanner } from "@/components/auth/SessionRevokedBanner";
import { AuthShell } from "@/components/flit/AuthShell";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput, PasswordInput } from "@/components/flit/Input";
import { FlitLink } from "@/components/flit/Link";
import { AlertCard } from "@/components/flit/Alert";

function LoginForm() {
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
        setError("Credenciales inválidas. Verifica tu correo y contraseña.");
        return;
      }
      const target =
        params.get("reason") === "session_revoked" ? "/?revoked=1" : "/";
      window.location.assign(target);
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthShell
      title="Iniciar sesión"
      subtitle="Accede con tu correo corporativo"
      footer={
        <FlitLink href="/forgot-password">¿Olvidaste tu contraseña?</FlitLink>
      }
    >
      {params.get("reason") === "session_revoked" && (
        <div className="mb-6">
          <SessionRevokedBanner />
        </div>
      )}
      <form onSubmit={onSubmit} className="space-y-6">
        <FlitInput
          label="Correo"
          name="email"
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
          spellCheck={false}
          placeholder="nombre@empresa.com…"
        />
        <PasswordInput
          label="Contraseña"
          name="password"
          id="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />
        {error && <AlertCard variant="danger">{error}</AlertCard>}
        <GradientButton type="submit" fullWidth disabled={loading}>
          {loading ? "Entrando…" : "Entrar"}
        </GradientButton>
      </form>
    </AuthShell>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
