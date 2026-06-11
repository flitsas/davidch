"use client";

import Link from "next/link";
import type { CompanyTab } from "@/lib/admin/companies-types";
import { COMPANY_TABS } from "@/lib/admin/companies-types";

export function CompanyTabs({
  companyId,
  activeTab,
}: {
  companyId: string;
  activeTab: CompanyTab;
}) {
  return (
    <nav
      className="flex flex-wrap gap-2 border-b border-flit-border-soft"
      aria-label="Secciones de configuración"
    >
      {COMPANY_TABS.map((tab) => {
        const isActive = tab.id === activeTab;
        return (
          <Link
            key={tab.id}
            href={`/admin/companies/${companyId}?tab=${tab.id}`}
            className={[
              "rounded-t-flit-md px-4 py-2 text-sm font-semibold transition-colors",
              isActive
                ? "border border-b-0 border-flit-border-soft bg-flit-bg-card text-flit-text-brand"
                : "text-flit-text-secondary hover:bg-flit-bg-modal/60 hover:text-flit-text-primary",
            ].join(" ")}
            aria-current={isActive ? "page" : undefined}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
