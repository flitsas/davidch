export type OtStatus = 0 | 1;

export type OtIndexItem = {
  id: string;
  tenantId: string;
  divipolCode: string;
  displayName: string;
  status: OtStatus;
  createdAt: string;
  updatedAt: string;
};

export type OtIndexResponse = {
  items: OtIndexItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type OtIndexSearchParams = {
  page?: string;
  pageSize?: string;
  sort?: string;
  divipol?: string;
  name?: string;
  id?: string;
  auditFrom?: string;
  auditTo?: string;
};

export type OtTenantSummary = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
};

export type OtDetail = {
  id: string;
  tenantId: string;
  divipolCode: string;
  displayName: string;
  status: OtStatus;
  integrationMode: number;
  createdAt: string;
  updatedAt: string;
  tenant: OtTenantSummary;
};

export type OtTab = "perfil" | "integracion" | "documentos";

export const OT_TABS: { id: OtTab; label: string }[] = [
  { id: "perfil", label: "Perfil" },
  { id: "integracion", label: "Integración" },
  { id: "documentos", label: "Documentos" },
];

export function parseOtTab(tab: string | undefined): OtTab {
  const found = OT_TABS.find((t) => t.id === tab);
  return found?.id ?? "perfil";
}
