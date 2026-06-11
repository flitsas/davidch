import { AlertCard } from "@/components/flit/Alert";

export function SessionRevokedBanner() {
  return (
    <AlertCard variant="warning">
      Tu sesión fue cerrada por un cambio de permisos. Inicia sesión de nuevo.
    </AlertCard>
  );
}
