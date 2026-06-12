"use client";

import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import type { DashboardCategory, DashboardCategoryKey } from "@/lib/tramites/dashboard-types";

const COLORS: Record<DashboardCategoryKey, string> = {
  matriculas: "#2563eb",
  traspasos: "#059669",
  otros: "#d97706",
};

type Props = {
  categories: DashboardCategory[];
  selectedCategory: DashboardCategoryKey | null;
  onCategorySelect: (key: DashboardCategoryKey) => void;
};

export function CategoryDonutChart({ categories, selectedCategory, onCategorySelect }: Props) {
  const data = categories.filter((c) => c.count > 0);

  if (data.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-flit-text-secondary">
        No hay trámites en el rango seleccionado.
      </p>
    );
  }

  return (
    <div data-testid="dashboard-donut-chart" className="h-80 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={data}
            dataKey="count"
            nameKey="label"
            cx="50%"
            cy="50%"
            innerRadius={60}
            outerRadius={100}
            paddingAngle={2}
            onClick={(_, index) => {
              const item = data[index];
              if (item) onCategorySelect(item.key);
            }}
            style={{ cursor: "pointer" }}
          >
            {data.map((entry) => (
              <Cell
                key={entry.key}
                fill={COLORS[entry.key]}
                stroke={selectedCategory === entry.key ? "#1e293b" : "transparent"}
                strokeWidth={selectedCategory === entry.key ? 3 : 0}
              />
            ))}
          </Pie>
          <Tooltip
            formatter={(value, _name, item) => {
              const row = item?.payload as DashboardCategory | undefined;
              if (!row) return String(value ?? "");
              return [`${row.count} (${row.percent.toFixed(1)}%)`, row.label];
            }}
          />
        </PieChart>
      </ResponsiveContainer>
      <ul className="mt-4 flex flex-wrap justify-center gap-4 text-sm">
        {categories.map((c) => (
          <li key={c.key} className="flex items-center gap-2">
            <span
              className="inline-block h-3 w-3 rounded-full"
              style={{ backgroundColor: COLORS[c.key] }}
            />
            <button
              type="button"
              className="font-medium text-flit-text-primary hover:underline"
              onClick={() => onCategorySelect(c.key)}
            >
              {c.label} ({c.count})
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
