import Link from "next/link";
import type { OtIndexSearchParams } from "@/lib/admin/ot-types";
import { buildOtIndexQuery } from "@/lib/admin/ot-api";

type Props = {
  page: number;
  pageSize: number;
  totalCount: number;
  searchParams: OtIndexSearchParams;
};

export function OtPagination({ page, pageSize, totalCount, searchParams }: Props) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages <= 1) return null;

  const prevQuery = buildOtIndexQuery({
    ...searchParams,
    page: String(Math.max(1, page - 1)),
    pageSize: String(pageSize),
  });
  const nextQuery = buildOtIndexQuery({
    ...searchParams,
    page: String(Math.min(totalPages, page + 1)),
    pageSize: String(pageSize),
  });

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-flit-border-soft px-6 py-4">
      <p className="text-sm text-flit-text-secondary">
        Página {page} de {totalPages} · {totalCount} OT
        {totalCount === 1 ? "" : "s"}
      </p>
      <div className="flex gap-2">
        {page > 1 ? (
          <Link
            href={`/admin/ot?${prevQuery}`}
            className="flit-focus-ring inline-flex min-h-10 items-center rounded-flit-pill border border-flit-border-soft bg-flit-bg-card px-4 text-sm font-semibold text-flit-text-primary hover:bg-flit-bg-modal"
          >
            Anterior
          </Link>
        ) : (
          <span className="inline-flex min-h-10 items-center rounded-flit-pill border border-flit-border-soft px-4 text-sm text-flit-text-muted opacity-50">
            Anterior
          </span>
        )}
        {page < totalPages ? (
          <Link
            href={`/admin/ot?${nextQuery}`}
            className="flit-focus-ring inline-flex min-h-10 items-center rounded-flit-pill border border-flit-border-soft bg-flit-bg-card px-4 text-sm font-semibold text-flit-text-primary hover:bg-flit-bg-modal"
          >
            Siguiente
          </Link>
        ) : (
          <span className="inline-flex min-h-10 items-center rounded-flit-pill border border-flit-border-soft px-4 text-sm text-flit-text-muted opacity-50">
            Siguiente
          </span>
        )}
      </div>
    </div>
  );
}
