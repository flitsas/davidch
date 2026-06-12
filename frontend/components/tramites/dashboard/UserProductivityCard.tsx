"use client";

type Props = {
  displayName: string;
  email: string;
  count: number;
  onRemove?: () => void;
};

export function UserProductivityCard({ displayName, email, count, onRemove }: Props) {
  return (
    <div className="rounded-flit-md border border-flit-border-soft bg-flit-bg-card p-4 shadow-sm">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="truncate font-semibold text-flit-text-brand">{displayName}</p>
          <p className="truncate text-xs text-flit-text-secondary">{email}</p>
        </div>
        {onRemove ? (
          <button
            type="button"
            onClick={onRemove}
            className="shrink-0 text-xs text-flit-text-secondary hover:text-flit-danger"
            aria-label="Quitar usuario"
          >
            ✕
          </button>
        ) : null}
      </div>
      <p className="mt-3 text-2xl font-bold text-flit-text-primary">
        {count > 0 ? count : "—"}
      </p>
      {count === 0 ? (
        <p className="mt-1 text-xs text-flit-text-secondary">
          Este usuario no ha radicado ningún trámite
        </p>
      ) : (
        <p className="mt-1 text-xs text-flit-text-secondary">trámites en el periodo</p>
      )}
    </div>
  );
}
