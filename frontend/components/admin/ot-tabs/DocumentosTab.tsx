"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { DocumentOrderList } from "@/components/ot/DocumentOrderList";
import type { DocumentOrderItem, ProcedureTypeSummary } from "@/lib/ot/settings-api";
import {
  documentOrderPath,
  fetchProcedureTypes,
  toDocumentOrderPayload,
} from "@/lib/ot/settings-api";

type Props = {
  otId?: string;
  apiBase: "admin" | "settings";
};

export function DocumentosTab({ otId, apiBase }: Props) {
  const router = useRouter();
  const [procedures, setProcedures] = useState<ProcedureTypeSummary[]>([]);
  const [procedureCode, setProcedureCode] = useState("");
  const [items, setItems] = useState<DocumentOrderItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const itemsRef = useRef(items);
  itemsRef.current = items;

  useEffect(() => {
    let cancelled = false;

    async function loadProcedures() {
      setLoading(true);
      setError(null);
      try {
        const types = await fetchProcedureTypes(apiBase);
        if (cancelled) {
          return;
        }
        setProcedures(types);
        setProcedureCode((current) => current || types[0]?.code || "");
      } catch (loadError) {
        if (!cancelled) {
          setError(loadError instanceof Error ? loadError.message : "No se pudieron cargar los trámites");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadProcedures();
    return () => {
      cancelled = true;
    };
  }, [apiBase]);

  useEffect(() => {
    if (!procedureCode) {
      return;
    }

    let cancelled = false;

    async function loadOrder() {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(documentOrderPath(apiBase, procedureCode, otId), {
          credentials: "include",
        });
        if (!res.ok) {
          const data = await res.json().catch(() => ({}));
          if (!cancelled) {
            setError(data.message ?? data.code ?? "No se pudo cargar el orden de documentos");
          }
          return;
        }

        const data = (await res.json()) as { items: DocumentOrderItem[] };
        if (!cancelled) {
          setItems(data.items);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadOrder();
    return () => {
      cancelled = true;
    };
  }, [apiBase, otId, procedureCode]);

  async function onSave() {
    setSaving(true);
    setError(null);

    try {
      const res = await fetch(documentOrderPath(apiBase, procedureCode, otId), {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ items: toDocumentOrderPayload(itemsRef.current) }),
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.message ?? data.code ?? "No se pudo guardar el orden de documentos");
        return;
      }

      const data = (await res.json()) as { items: DocumentOrderItem[] };
      setItems(data.items);
      router.refresh();
    } finally {
      setSaving(false);
    }
  }

  if (loading && procedures.length === 0) {
    return <p className="text-sm text-flit-text-secondary">Cargando trámites…</p>;
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <label htmlFor="procedure-type" className="text-sm font-semibold text-flit-text-brand">
          Tipo de trámite (RF09)
        </label>
        <select
          id="procedure-type"
          data-testid="procedure-type-select"
          className="w-full max-w-md rounded-lg border border-flit-border bg-flit-surface px-3 py-2 text-sm"
          value={procedureCode}
          onChange={(event) => setProcedureCode(event.target.value)}
        >
          {procedures.map((procedure) => (
            <option key={procedure.code} value={procedure.code}>
              {procedure.name}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <p className="text-sm text-flit-text-secondary">Cargando documentos…</p>
      ) : (
        <DocumentOrderList
          items={items}
          onChange={setItems}
          onSave={onSave}
          saving={saving}
          error={error}
        />
      )}
    </div>
  );
}
