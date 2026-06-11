type ChipVariant = "success" | "active" | "warning" | "danger" | "draft";

const chipStyles: Record<ChipVariant, string> = {
  success: "bg-flit-success/15 text-flit-success",
  active: "bg-flit-active/15 text-flit-active",
  warning: "bg-flit-warning/15 text-flit-warning",
  danger: "bg-flit-danger/15 text-flit-danger",
  draft: "bg-flit-draft/15 text-flit-draft",
};

export function StatusChip({
  label,
  variant = "draft",
}: {
  label: string;
  variant?: ChipVariant;
}) {
  return (
    <span
      className={[
        "inline-flex items-center rounded-flit-pill px-3 py-1 text-xs font-semibold uppercase tracking-wide",
        chipStyles[variant],
      ].join(" ")}
    >
      {label}
    </span>
  );
}

export function statusVariantFromUserStatus(status: string): ChipVariant {
  const normalized = status.toLowerCase();
  if (normalized === "active" || normalized === "activo") return "success";
  if (normalized === "pending" || normalized === "pendiente") return "warning";
  if (normalized === "blocked" || normalized === "bloqueado") return "danger";
  return "draft";
}
