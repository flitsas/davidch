import type { TramitesIndexSearchParams } from "./types";

export function buildTramitesIndexQuery(params: TramitesIndexSearchParams): string {
  const page = params.page && Number(params.page) > 0 ? params.page : "1";
  const pageSize = params.pageSize && Number(params.pageSize) > 0 ? params.pageSize : "20";
  return new URLSearchParams({ page, pageSize }).toString();
}

export function tramiteStatusLabel(status: string): string {
  switch (status) {
    case "PendienteEnvio":
      return "Pendiente de envío";
    default:
      return status;
  }
}
