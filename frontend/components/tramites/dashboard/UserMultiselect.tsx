"use client";

import { useEffect, useState } from "react";
import { buildDashboardQuery, fetchDashboardUsers } from "@/lib/tramites/dashboard-api";
import type { DashboardUserSearchItem } from "@/lib/tramites/dashboard-types";

type Props = {
  baseQuery: string;
  excludedUserIds: Set<string>;
  onSelect: (user: DashboardUserSearchItem) => void;
};

export function UserMultiselect({ baseQuery, excludedUserIds, onSelect }: Props) {
  const [q, setQ] = useState("");
  const [results, setResults] = useState<DashboardUserSearchItem[]>([]);
  const [loading, setLoading] = useState(false);

  const trimmedQ = q.trim();
  const canSearch = trimmedQ.length >= 2;
  const displayedResults = canSearch ? results : [];

  useEffect(() => {
    if (!canSearch) return;

    const handle = window.setTimeout(() => {
      setLoading(true);
      const query = buildDashboardQuery({
        q: trimmedQ,
        page: 1,
        pageSize: 10,
      });
      const search = baseQuery ? `${baseQuery}&${query}` : query;
      void fetchDashboardUsers(search)
        .then((res) => setResults(res.items.filter((u) => !excludedUserIds.has(u.userId))))
        .catch(() => setResults([]))
        .finally(() => setLoading(false));
    }, 300);

    return () => window.clearTimeout(handle);
  }, [canSearch, trimmedQ, baseQuery, excludedUserIds]);

  return (
    <div className="relative">
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-flit-text-secondary">Buscar usuario</span>
        <input
          type="search"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Correo electrónico…"
          className="min-h-11 w-full rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 text-flit-text-primary"
        />
      </label>
      {canSearch && loading ? (
        <p className="mt-2 text-xs text-flit-text-secondary">Buscando…</p>
      ) : null}
      {displayedResults.length > 0 ? (
        <ul className="absolute z-10 mt-1 max-h-48 w-full overflow-auto rounded-flit-md border border-flit-border-soft bg-flit-bg-card shadow-lg">
          {displayedResults.map((user) => (
            <li key={user.userId}>
              <button
                type="button"
                className="w-full px-3 py-2 text-left text-sm hover:bg-flit-bg-modal"
                onClick={() => {
                  onSelect(user);
                  setQ("");
                  setResults([]);
                }}
              >
                {user.email}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
