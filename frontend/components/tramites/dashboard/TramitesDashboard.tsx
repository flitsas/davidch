"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { FlitCard } from "@/components/flit/Card";
import { CategoryDonutChart } from "./CategoryDonutChart";
import { DateRangeFilter } from "./DateRangeFilter";
import { DetailTablePanel } from "./DetailTablePanel";
import { TenantSelector } from "./TenantSelector";
import { UserMultiselect } from "./UserMultiselect";
import { UserProductivityCard } from "./UserProductivityCard";
import {
  buildDashboardQuery,
  dashboardExportUrl,
  defaultDashboardDateRange,
  fetchDashboardDetail,
  fetchDashboardSummary,
  fetchDashboardUserStats,
  fetchDashboardUsersTop,
} from "@/lib/tramites/dashboard-api";
import type {
  DashboardCategoryKey,
  DashboardDetailRow,
  DashboardSummary,
  DashboardUserSearchItem,
  DashboardUserStat,
} from "@/lib/tramites/dashboard-types";

type Props = {
  isSuperAdmin: boolean;
  defaultTenantId: string | null;
};

export function TramitesDashboard({ isSuperAdmin, defaultTenantId }: Props) {
  const defaults = defaultDashboardDateRange();
  const [from, setFrom] = useState(defaults.from);
  const [to, setTo] = useState(defaults.to);
  const [tenantId, setTenantId] = useState(isSuperAdmin ? "" : (defaultTenantId ?? ""));
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [topUsers, setTopUsers] = useState<DashboardUserStat[]>([]);
  const [extraCards, setExtraCards] = useState<DashboardUserStat[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<DashboardCategoryKey | null>(null);
  const [detailRows, setDetailRows] = useState<DashboardDetailRow[]>([]);
  const [detailTotal, setDetailTotal] = useState(0);
  const [detailLoading, setDetailLoading] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const baseQuery = useMemo(
    () => buildDashboardQuery({ from, to, tenantId: tenantId || undefined }),
    [from, to, tenantId],
  );

  const excludedUserIds = useMemo(() => {
    const ids = new Set<string>();
    for (const u of topUsers) ids.add(u.userId);
    for (const u of extraCards) ids.add(u.userId);
    return ids;
  }, [topUsers, extraCards]);

  const loadOverview = useCallback(async () => {
    setLoading(true);
    setError(null);
    setSelectedCategory(null);
    setDetailRows([]);
    try {
      const [summaryRes, topRes] = await Promise.all([
        fetchDashboardSummary(baseQuery),
        fetchDashboardUsersTop(baseQuery),
      ]);
      setSummary(summaryRes);
      setTopUsers(topRes.items);
      setExtraCards([]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar el dashboard.");
      setSummary(null);
      setTopUsers([]);
    } finally {
      setLoading(false);
    }
  }, [baseQuery]);

  useEffect(() => {
    void loadOverview();
  }, [loadOverview]);

  useEffect(() => {
    if (!selectedCategory) {
      setDetailRows([]);
      setDetailTotal(0);
      return;
    }

    setDetailLoading(true);
    const query = buildDashboardQuery({
      from,
      to,
      tenantId: tenantId || undefined,
      category: selectedCategory,
      page: 1,
      pageSize: 50,
    });
    void fetchDashboardDetail(query)
      .then((res) => {
        setDetailRows(res.items);
        setDetailTotal(res.totalCount);
      })
      .catch((e) => {
        setError(e instanceof Error ? e.message : "No se pudo cargar el detalle.");
        setDetailRows([]);
        setDetailTotal(0);
      })
      .finally(() => setDetailLoading(false));
  }, [selectedCategory, from, to, tenantId]);

  async function handleAddUser(user: DashboardUserSearchItem) {
    try {
      const stats = await fetchDashboardUserStats(user.userId, baseQuery);
      setExtraCards((prev) => [
        ...prev,
        {
          userId: user.userId,
          displayName: user.email,
          email: user.email,
          count: stats.count,
        },
      ]);
    } catch {
      setError("No se pudo cargar las estadísticas del usuario.");
    }
  }

  function handleExport() {
    if (!selectedCategory) return;
    const url = dashboardExportUrl(selectedCategory, baseQuery);
    window.open(url, "_blank", "noopener,noreferrer");
  }

  const categories = summary?.categories ?? [];

  return (
    <div className="space-y-6" data-testid="tramites-dashboard">
      <FlitCard className="space-y-4">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <DateRangeFilter from={from} to={to} onFromChange={setFrom} onToChange={setTo} />
          {isSuperAdmin ? <TenantSelector value={tenantId} onChange={setTenantId} /> : null}
        </div>
        {summary ? (
          <p className="text-sm text-flit-text-secondary">
            Total en periodo: <strong className="text-flit-text-primary">{summary.total}</strong>{" "}
            trámites ({summary.from} — {summary.to})
          </p>
        ) : null}
      </FlitCard>

      {error ? (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      ) : null}

      {loading ? (
        <p className="text-sm text-flit-text-secondary">Cargando métricas…</p>
      ) : (
        <div className="flex flex-col gap-6 lg:flex-row">
          {selectedCategory ? (
            <FlitCard className="lg:w-1/2">
              <DetailTablePanel
                category={selectedCategory}
                rows={detailRows}
                totalCount={detailTotal}
                loading={detailLoading}
                onExport={handleExport}
              />
            </FlitCard>
          ) : null}

          <FlitCard className={selectedCategory ? "lg:w-1/2" : "w-full"}>
            <h2 className="mb-4 text-sm font-semibold text-flit-text-brand">
              Distribución por tipo de trámite
            </h2>
            <CategoryDonutChart
              categories={categories}
              selectedCategory={selectedCategory}
              onCategorySelect={setSelectedCategory}
            />
          </FlitCard>
        </div>
      )}

      <FlitCard className="space-y-4">
        <h2 className="text-sm font-semibold text-flit-text-brand">Productividad por usuario</h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {topUsers.map((user) => (
            <UserProductivityCard
              key={user.userId}
              displayName={user.displayName}
              email={user.email}
              count={user.count}
            />
          ))}
          {extraCards.map((user) => (
            <UserProductivityCard
              key={user.userId}
              displayName={user.displayName}
              email={user.email}
              count={user.count}
              onRemove={() => setExtraCards((prev) => prev.filter((c) => c.userId !== user.userId))}
            />
          ))}
        </div>

        {isSuperAdmin && !tenantId ? (
          <p className="text-sm text-flit-text-secondary">
            Seleccione una compañía para buscar usuarios adicionales.
          </p>
        ) : (
          <UserMultiselect
            baseQuery={baseQuery}
            excludedUserIds={excludedUserIds}
            onSelect={(user) => void handleAddUser(user)}
          />
        )}
      </FlitCard>
    </div>
  );
}
