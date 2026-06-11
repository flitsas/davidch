import type { ReactNode } from "react";

export function FlitCard({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={[
        "rounded-flit-lg border border-flit-border-soft bg-flit-bg-card p-6 shadow-flit-card",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {children}
    </div>
  );
}

export function PageHeaderCard({
  title,
  subtitle,
  actions,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}) {
  return (
    <FlitCard className="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 className="text-pretty text-2xl font-bold text-flit-text-brand">
          {title}
        </h1>
        {subtitle && (
          <p className="mt-1 text-sm text-flit-text-secondary">{subtitle}</p>
        )}
      </div>
      {actions}
    </FlitCard>
  );
}
