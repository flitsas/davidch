export type DashboardCategoryKey = "matriculas" | "traspasos" | "otros";

export type DashboardCategory = {
  key: DashboardCategoryKey;
  label: string;
  count: number;
  percent: number;
};

export type DashboardSummary = {
  from: string;
  to: string;
  total: number;
  categories: DashboardCategory[];
};

export type DashboardDetailRow = {
  id: string;
  procedureInstanceId: string;
  radicatedAt: string;
  status: string;
  plate: string;
  ownerName: string;
  updatedAt: string;
};

export type DashboardDetailResponse = {
  items: DashboardDetailRow[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type DashboardUserStat = {
  userId: string;
  displayName: string;
  email: string;
  count: number;
};

export type DashboardUsersTopResponse = {
  items: DashboardUserStat[];
};

export type DashboardUserSearchItem = {
  userId: string;
  email: string;
};

export type DashboardUsersSearchResponse = {
  items: DashboardUserSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type DashboardUserStatsResponse = {
  userId: string;
  count: number;
};

export type DashboardTenantOption = {
  tenantId: string;
  label: string;
};
