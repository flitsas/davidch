"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthShell } from "@/components/flit/AuthShell";
import { GradientButton } from "@/components/flit/Button";
import { PasswordInput } from "@/components/flit/Input";
import { FlitLink } from "@/components/flit/Link";
import { AlertCard } from "@/components/flit/Alert";

function ActivateForm() {
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
      const res = await fetch("/api/auth/activate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, password }),
        credentials: "include",
      });
      if (!res.ok) {
        const body = await res.json().catch(() => ({}));
        setError(body.message ?? "No se pudo activar la cuenta");
        return;
      }
      router.push("/login");
    } finally {
      setLoading(false);
    }
  }

  if (!token) {
    return (
      <AuthShell title="Enlace inválido">
        <AlertCard variant="danger">
          El enlace de activación no es válido. Contacta al administrador para recibir una nueva
          invitación.
        </AlertCard>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      title="Activar cuenta"
      subtitle="Define tu contraseña para completar el registro"
      footer={<FlitLink href="/login">Ir a iniciar sesión</FlitLink>}
    >
      <form onSubmit={onSubmit} className="space-y-6">
        <PasswordInput
          label="Nueva contraseña"
          name="password"
          id="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
          autoComplete="new-password"
          placeholder="Mínimo 8 caracteres…"
        />
        <PasswordInput
          label="Confirmar contraseña"
          name="confirm"
          id="confirm"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          required
          minLength={8}
          autoComplete="new-password"
          error={error}
        />
        <GradientButton type="submit" fullWidth disabled={loading}>
          {loading ? "Activando…" : "Activar cuenta"}
        </GradientButton>
      </form>
    </AuthShell>
  );
}

export default function ActivatePage() {
  return (
    <Suspense>
      <ActivateForm />
    </Suspense>
  );
}
