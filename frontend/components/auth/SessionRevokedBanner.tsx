export function SessionRevokedBanner() {
  return (
    <p
      role="alert"
      className="mb-4 rounded border border-amber-300 bg-amber-50 p-3 text-amber-900"
    >
      Tu sesión fue cerrada por un cambio de permisos. Inicia sesión de nuevo.
    </p>
  );
}
