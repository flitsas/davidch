import type {
  DashboardDetailResponse,
  DashboardSummary,
  DashboardUserStatsResponse,
  DashboardUsersSearchResponse,
  DashboardUsersTopResponse,
} from "./dashboard-types";

export type DashboardQueryParams = {
  from?: string;
  to?: string;
  tenantId?: string;
  category?: string;
  page?: number;
  pageSize?: number;
  q?: string;
};

async function parseJson<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const body = (await res.json().catch(() => ({}))) as { message?: string };
    throw new Error(body.message ?? `HTTP ${res.status}`);
  }
  return res.json() as Promise<T>;
}

export function buildDashboardQuery(params: DashboardQueryParams): string {
  const search = new URLSearchParams();
  if (params.from) search.set("from", params.from);
  if (params.to) search.set("to", params.to);
  if (params.tenantId) search.set("tenantId", params.tenantId);
  if (params.category) search.set("category", params.category);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  if (params.q) search.set("q", params.q);
  return search.toString();
}

export function defaultDashboardDateRange(): { from: string; to: string } {
  const to = new Date();
  const from = new Date();
  from.setUTCDate(from.getUTCDate() - 30);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

export async function fetchDashboardSummary(query: string): Promise<DashboardSummary> {
  const res = await fetch(`/api/v1/tramites/dashboard/summary?${query}`, {
    credentials: "include",
  });
  return parseJson(res);
}

export async function fetchDashboardDetail(query: string): Promise<DashboardDetailResponse> {
  const res = await fetch(`/api/v1/tramites/dashboard/detail?${query}`, {
    credentials: "include",
  });
  return parseJson(res);
}

export async function fetchDashboardUsersTop(query: string): Promise<DashboardUsersTopResponse> {
  const res = await fetch(`/api/v1/tramites/dashboard/users/top?${query}`, {
    credentials: "include",
  });
  return parseJson(res);
}

export async function fetchDashboardUsers(query: string): Promise<DashboardUsersSearchResponse> {
  const res = await fetch(`/api/v1/tramites/dashboard/users?${query}`, {
    credentials: "include",
  });
  return parseJson(res);
}

export async function fetchDashboardUserStats(
  userId: string,
  query: string,
): Promise<DashboardUserStatsResponse> {
  const res = await fetch(`/api/v1/tramites/dashboard/users/${userId}/stats?${query}`, {
    credentials: "include",
  });
  return parseJson(res);
}

export function dashboardExportUrl(category: string, query: string): string {
  const params = new URLSearchParams(query);
  params.set("category", category);
  return `/api/v1/tramites/dashboard/export?${params.toString()}`;
}
