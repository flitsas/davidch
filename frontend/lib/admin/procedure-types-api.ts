import type { ProcedureTypeIndexSearchParams } from "./procedure-types-types";

export function buildProcedureTypeIndexQuery(params: ProcedureTypeIndexSearchParams): string {
  const query = new URLSearchParams();
  const page = params.page ? Number(params.page) : 1;
  const pageSize = params.pageSize ? Number(params.pageSize) : 20;

  query.set("page", String(Math.max(1, page)));
  query.set("pageSize", String(Math.min(100, Math.max(1, pageSize))));
  query.set("sort", params.sort?.trim() || "updatedAt:desc");

  if (params.name?.trim()) query.set("name", params.name.trim());
  if (params.isActive?.trim()) query.set("isActive", params.isActive.trim());

  return query.toString();
}

export function vehicleQueryModeLabel(mode: string | number): string {
  if (mode === "Plate" || mode === 0) return "Placa (RUNT)";
  return "VIN (RUNT)";
}

export function procedureTypeStatusLabel(isActive: boolean): string {
  return isActive ? "Activo" : "Inactivo";
}
