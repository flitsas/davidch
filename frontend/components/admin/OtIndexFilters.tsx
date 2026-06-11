"use client";

import { useRouter } from "next/navigation";
import { FlitButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";
import type { OtIndexSearchParams } from "@/lib/admin/ot-types";

type Props = {
  initial: OtIndexSearchParams;
};

export function OtIndexFilters({ initial }: Props) {
  const router = useRouter();

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const params = new URLSearchParams();

    const id = String(form.get("id") ?? "").trim();
    const divipol = String(form.get("divipol") ?? "").trim();
    const name = String(form.get("name") ?? "").trim();
    const auditFrom = String(form.get("auditFrom") ?? "").trim();
    const auditTo = String(form.get("auditTo") ?? "").trim();

    if (id) params.set("id", id);
    if (divipol) params.set("divipol", divipol);
    if (name) params.set("name", name);
    if (auditFrom) params.set("auditFrom", toAuditIso(auditFrom, false));
    if (auditTo) params.set("auditTo", toAuditIso(auditTo, true));

    params.set("page", "1");
    params.set("pageSize", initial.pageSize ?? "20");
    params.set("sort", initial.sort ?? "createdAt:desc");

    router.push(`/admin/ot?${params.toString()}`);
  }

  function handleClear() {
    router.push("/admin/ot");
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5"
    >
      <FlitInput
        label="ID OT"
        name="id"
        defaultValue={initial.id ?? ""}
        placeholder="UUID"
        autoComplete="off"
      />
      <FlitInput
        label="DIVIPOL"
        name="divipol"
        defaultValue={initial.divipol ?? ""}
        placeholder="11001000"
        autoComplete="off"
      />
      <FlitInput
        label="Nombre"
        name="name"
        defaultValue={initial.name ?? ""}
        placeholder="Nombre del OT"
        autoComplete="off"
      />
      <FlitInput
        label="Auditoría desde"
        name="auditFrom"
        type="date"
        defaultValue={toDateInputValue(initial.auditFrom)}
      />
      <FlitInput
        label="Auditoría hasta"
        name="auditTo"
        type="date"
        defaultValue={toDateInputValue(initial.auditTo)}
      />
      <div className="flex flex-wrap items-end gap-3 sm:col-span-2 lg:col-span-3 xl:col-span-5">
        <FlitButton type="submit" variant="primary">
          Buscar
        </FlitButton>
        <FlitButton type="button" variant="ghost" onClick={handleClear}>
          Limpiar
        </FlitButton>
      </div>
    </form>
  );
}

function toDateInputValue(iso?: string): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toISOString().slice(0, 10);
}

function toAuditIso(dateValue: string, endOfDay: boolean): string {
  const [y, m, d] = dateValue.split("-").map(Number);
  if (!y || !m || !d) return dateValue;
  const date = endOfDay
    ? new Date(Date.UTC(y, m - 1, d, 23, 59, 59, 999))
    : new Date(Date.UTC(y, m - 1, d, 0, 0, 0, 0));
  return date.toISOString();
}
