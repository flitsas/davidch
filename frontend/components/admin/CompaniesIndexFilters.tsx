"use client";

import { useRouter } from "next/navigation";
import { FlitButton } from "@/components/flit/Button";
import { FlitInput } from "@/components/flit/Input";
import type { CompaniesIndexSearchParams } from "@/lib/admin/companies-types";

type Props = {
  initial: CompaniesIndexSearchParams;
};

export function CompaniesIndexFilters({ initial }: Props) {
  const router = useRouter();

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const params = new URLSearchParams();

    const id = String(form.get("id") ?? "").trim();
    const nit = String(form.get("nit") ?? "").trim();
    const name = String(form.get("name") ?? "").trim();
    const auditFrom = String(form.get("auditFrom") ?? "").trim();
    const auditTo = String(form.get("auditTo") ?? "").trim();

    if (id) params.set("id", id);
    if (nit) params.set("nit", nit);
    if (name) params.set("name", name);
    if (auditFrom) params.set("auditFrom", toAuditIso(auditFrom, false));
    if (auditTo) params.set("auditTo", toAuditIso(auditTo, true));

    params.set("page", "1");
    params.set("pageSize", initial.pageSize ?? "20");
    params.set("sort", initial.sort ?? "createdAt:desc");

    router.push(`/admin/companies?${params.toString()}`);
  }

  function handleClear() {
    router.push("/admin/companies");
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5"
    >
      <FlitInput
        label="ID compañía"
        name="id"
        defaultValue={initial.id ?? ""}
        placeholder="UUID"
        autoComplete="off"
      />
      <FlitInput
        label="NIT"
        name="nit"
        defaultValue={initial.nit ?? ""}
        placeholder="900123456-1"
        autoComplete="off"
      />
      <FlitInput
        label="Nombre"
        name="name"
        defaultValue={initial.name ?? ""}
        placeholder="Razón social"
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
