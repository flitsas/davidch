"use client";

type Props = {
  from: string;
  to: string;
  onFromChange: (value: string) => void;
  onToChange: (value: string) => void;
};

export function DateRangeFilter({ from, to, onFromChange, onToChange }: Props) {
  return (
    <div className="flex flex-wrap items-end gap-4">
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-flit-text-secondary">Desde</span>
        <input
          type="date"
          value={from}
          onChange={(e) => onFromChange(e.target.value)}
          className="min-h-11 rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 text-flit-text-primary"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-flit-text-secondary">Hasta</span>
        <input
          type="date"
          value={to}
          onChange={(e) => onToChange(e.target.value)}
          className="min-h-11 rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 text-flit-text-primary"
        />
      </label>
    </div>
  );
}
