import type { ReactNode } from "react";

type AlertVariant = "warning" | "danger" | "success" | "info";

const alertStyles: Record<AlertVariant, string> = {
  warning: "border-flit-warning/40 bg-flit-warning/10 text-flit-text-primary",
  danger: "border-flit-danger/40 bg-flit-danger/10 text-flit-text-primary",
  success: "border-flit-success/40 bg-flit-success/10 text-flit-text-primary",
  info: "border-flit-active/40 bg-flit-active/10 text-flit-text-primary",
};

export function AlertCard({
  children,
  variant = "info",
}: {
  children: ReactNode;
  variant?: AlertVariant;
}) {
  return (
    <p
      role="alert"
      aria-live="polite"
      className={[
        "rounded-flit-md border px-4 py-3 text-sm",
        alertStyles[variant],
      ].join(" ")}
    >
      {children}
    </p>
  );
}
