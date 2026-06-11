import type { CompaniesIndexSearchParams } from "./companies-types";

export function buildCompaniesIndexQuery(
  params: CompaniesIndexSearchParams
): string {
  const query = new URLSearchParams();
  const page = params.page ? Number(params.page) : 1;
  const pageSize = params.pageSize ? Number(params.pageSize) : 20;

  query.set("page", String(Math.max(1, page)));
  query.set("pageSize", String(Math.min(100, Math.max(1, pageSize))));
  query.set("sort", params.sort?.trim() || "createdAt:desc");

  if (params.id?.trim()) query.set("id", params.id.trim());
  if (params.nit?.trim()) query.set("nit", params.nit.trim());
  if (params.name?.trim()) query.set("name", params.name.trim());
  if (params.auditFrom?.trim()) query.set("auditFrom", params.auditFrom.trim());
  if (params.auditTo?.trim()) query.set("auditTo", params.auditTo.trim());

  return query.toString();
}

export function companyStatusLabel(status: number): string {
  return status === 1 ? "Suspendida" : "Activa";
}

export function companyStatusApiValue(status: number): "Active" | "Suspended" {
  return status === 1 ? "Suspended" : "Active";
}
