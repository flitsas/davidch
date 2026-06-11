"use client";

import { useState } from "react";
import { AuthShell } from "@/components/flit/AuthShell";
import { GradientButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";
import { FlitLink } from "@/components/flit/Link";
import { AlertCard } from "@/components/flit/Alert";

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
    <AuthShell
      title="Recuperar contraseña"
      subtitle="Te enviaremos un enlace si el correo está registrado"
      footer={<FlitLink href="/login">Volver al inicio de sesión</FlitLink>}
    >
      {sent ? (
        <AlertCard variant="success">
          Si el correo existe en el sistema, recibirás un enlace para restablecer tu contraseña.
        </AlertCard>
      ) : (
        <form onSubmit={onSubmit} className="space-y-6">
          <FlitInput
            label="Correo"
            name="email"
            id="email"
            type="email"
            placeholder="nombre@empresa.com…"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
            spellCheck={false}
          />
          <GradientButton type="submit" fullWidth disabled={loading}>
            {loading ? "Enviando…" : "Enviar enlace"}
          </GradientButton>
        </form>
      )}
    </AuthShell>
  );
}
