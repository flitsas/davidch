export type CompanyStatus = 0 | 1;

export type CompanyIndexItem = {
  id: string;
  tenantId: string;
  nit: string;
  legalName: string;
  status: CompanyStatus;
  createdAt: string;
  updatedAt: string;
};

export type CompanyIndexResponse = {
  items: CompanyIndexItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type CompaniesIndexSearchParams = {
  page?: string;
  pageSize?: string;
  sort?: string;
  nit?: string;
  name?: string;
  id?: string;
  auditFrom?: string;
  auditTo?: string;
};

export type CompanyTenantSummary = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
};

export type CompanyDetail = {
  id: string;
  tenantId: string;
  nit: string;
  legalName: string;
  status: CompanyStatus;
  createdAt: string;
  updatedAt: string;
  tenant: CompanyTenantSummary;
};

export type CompanyTab =
  | "matricula"
  | "traspasos"
  | "config-empresa"
  | "contingencia";

export const COMPANY_TABS: { id: CompanyTab; label: string }[] = [
  { id: "matricula", label: "Matrícula" },
  { id: "traspasos", label: "Traspasos" },
  { id: "config-empresa", label: "Config Empresa" },
  { id: "contingencia", label: "Contingencia" },
];

export function parseCompanyTab(tab: string | undefined): CompanyTab {
  const found = COMPANY_TABS.find((t) => t.id === tab);
  return found?.id ?? "matricula";
}
