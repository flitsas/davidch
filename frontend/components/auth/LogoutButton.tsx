"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { FlitButton } from "@/components/flit/Button";

export function LogoutButton() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  async function logout() {
    setLoading(true);
    try {
      await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include",
      });
      router.push("/login");
      router.refresh();
    } finally {
      setLoading(false);
    }
  }

  return (
    <FlitButton
      variant="ghost"
      onClick={logout}
      disabled={loading}
      className="min-h-9 px-4 text-sm"
      aria-label="Cerrar sesión"
    >
      {loading ? "Saliendo…" : "Salir"}
    </FlitButton>
  );
}
