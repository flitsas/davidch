import type {
  ActorFormState,
  DocumentFileState,
  ProcedureDefinition,
  ProcedureTypeSummary,
  TrafficAuthority,
} from "./types";

async function parseJson<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    const message = typeof body.message === "string" ? body.message : `HTTP ${res.status}`;
    throw new Error(message);
  }
  return res.json() as Promise<T>;
}

export async function fetchProcedureTypes(): Promise<ProcedureTypeSummary[]> {
  const res = await fetch("/api/v1/tramites/procedure-types", { credentials: "include" });
  return parseJson(res);
}

export async function fetchProcedureDefinition(id: string): Promise<ProcedureDefinition> {
  const res = await fetch(`/api/v1/tramites/procedure-types/${id}`, { credentials: "include" });
  return parseJson(res);
}

export async function fetchTrafficAuthorities(): Promise<TrafficAuthority[]> {
  const res = await fetch("/api/v1/tramites/traffic-authorities", { credentials: "include" });
  return parseJson(res);
}

export async function queryRunt(type: "placa" | "vin" | "conductor", q: string) {
  const res = await fetch(`/api/v1/runt/${type}?q=${encodeURIComponent(q)}`, {
    credentials: "include",
  });
  return parseJson<unknown>(res);
}

export async function lookupRues(nit: string) {
  const res = await fetch(`/api/v1/tramites/lookups/rues?nit=${encodeURIComponent(nit)}`, {
    credentials: "include",
  });
  return parseJson<unknown>(res);
}

export async function lookupSimit(documentType: string, documentNumber: string) {
  const params = new URLSearchParams({ documentType, documentNumber });
  const res = await fetch(`/api/v1/tramites/lookups/simit?${params}`, { credentials: "include" });
  return parseJson<unknown>(res);
}

export async function createTramite(payload: {
  procedureTypeId: string;
  otDivipolCode: string;
  vehicleQueryValue: string;
  actors: ActorFormState[];
}) {
  const res = await fetch("/api/v1/tramites", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      procedureTypeId: payload.procedureTypeId,
      otDivipolCode: payload.otDivipolCode,
      vehicleQueryValue: payload.vehicleQueryValue,
      actors: payload.actors.map((actor) => ({
        roleLabel: actor.roleLabel,
        sortOrder: actor.sortOrder,
        personKind: actor.personKind,
        documentType: actor.documentType,
        documentNumber: actor.documentNumber,
        legalRepresentative: actor.legalRepresentative ?? null,
      })),
    }),
  });
  return parseJson<{ id: string; status: string }>(res);
}

export async function uploadTramiteDocument(instanceId: string, label: string, file: File) {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(
    `/api/v1/tramites/${instanceId}/documents?label=${encodeURIComponent(label)}`,
    {
      method: "POST",
      credentials: "include",
      body: form,
    },
  );
  return parseJson(res);
}

export function staticDocuments(definition: ProcedureDefinition | null): DocumentFileState[] {
  if (!definition) return [];
  return definition.documents
    .filter((d) => d.kind === "Static" || d.kind === 0)
    .map((d) => ({ label: d.label, file: null }));
}

export function isVinMode(mode: ProcedureDefinition["vehicleQueryMode"]): boolean {
  return mode === "Vin" || mode === 1;
}
