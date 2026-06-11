export type IntegrationMode = "Dashboard" | "Qx";

export type IntegrationConfig = {
  integration_mode: IntegrationMode;
};

export type ProcedureTypeSummary = {
  code: string;
  name: string;
};

export type DocumentOrderItem = {
  document_type_code: string;
  document_type_name: string;
  position: number;
  is_included: boolean;
};

export type DocumentOrderResponse = {
  procedure_type_code: string;
  items: DocumentOrderItem[];
};

export type DocumentOrderItemInput = {
  document_type_code: string;
  position: number;
  is_included: boolean;
};

export const OT_PROCEDURE_TYPES: ProcedureTypeSummary[] = [
  { code: "MATRICULA_INICIAL", name: "Matrícula inicial" },
  { code: "TRASPASO", name: "Traspaso de propiedad" },
  { code: "RADICADO_CUENTA", name: "Radicado de cuenta" },
];

export function integrationModeLabel(mode: IntegrationMode): string {
  return mode === "Qx" ? "Quipux (QX)" : "Dashboard FLIT";
}

export function documentOrderPath(
  apiBase: "admin" | "settings",
  procedureCode: string,
  otId?: string,
): string {
  if (apiBase === "admin") {
    return `/api/v1/admin/ot/${otId}/document-order/${procedureCode}`;
  }
  return `/api/v1/ot/settings/document-order/${procedureCode}`;
}

export function procedureTypesPath(apiBase: "admin" | "settings"): string {
  if (apiBase === "admin") {
    return "";
  }
  return "/api/v1/ot/settings/procedure-types";
}

export async function fetchProcedureTypes(
  apiBase: "admin" | "settings",
): Promise<ProcedureTypeSummary[]> {
  if (apiBase === "admin") {
    return OT_PROCEDURE_TYPES;
  }

  const res = await fetch(procedureTypesPath(apiBase), { credentials: "include" });
  if (!res.ok) {
    throw new Error("No se pudieron cargar los tipos de trámite");
  }
  return (await res.json()) as ProcedureTypeSummary[];
}

export function normalizeDocumentOrderItems(items: DocumentOrderItem[]): DocumentOrderItem[] {
  const includedSorted = items
    .filter((item) => item.is_included)
    .toSorted((a, b) => a.position - b.position);

  const positionByCode = new Map(
    includedSorted.map((item, index) => [item.document_type_code, index + 1]),
  );

  return items.map((item) => {
    if (!item.is_included) {
      return item;
    }

    return { ...item, position: positionByCode.get(item.document_type_code) ?? item.position };
  });
}

export function toDocumentOrderPayload(items: DocumentOrderItem[]): DocumentOrderItemInput[] {
  return normalizeDocumentOrderItems(items).map((item) => ({
    document_type_code: item.document_type_code,
    position: item.position,
    is_included: item.is_included,
  }));
}

export function reorderIncludedItems(
  items: DocumentOrderItem[],
  activeId: string,
  overId: string,
): DocumentOrderItem[] {
  const included = items.filter((item) => item.is_included);
  const oldIndex = included.findIndex((item) => item.document_type_code === activeId);
  const newIndex = included.findIndex((item) => item.document_type_code === overId);
  if (oldIndex < 0 || newIndex < 0 || oldIndex === newIndex) {
    return items;
  }

  const reorderedIncluded = [...included];
  const [moved] = reorderedIncluded.splice(oldIndex, 1);
  reorderedIncluded.splice(newIndex, 0, moved);

  const includedByCode = new Map(
    reorderedIncluded.map((item, index) => [
      item.document_type_code,
      { ...item, position: index + 1 },
    ]),
  );

  return items.map((item) => includedByCode.get(item.document_type_code) ?? item);
}
