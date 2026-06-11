"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { FlitButton } from "@/components/flit/Button";
import { FlitCard } from "@/components/flit/Card";
import { FlitInput } from "@/components/flit/Input";
import { vehicleQueryModeLabel } from "@/lib/admin/procedure-types-api";
import type {
  DocumentKind,
  ProcedureTypeDetail,
  SaveProcedureTypePayload,
  VehicleQueryMode,
} from "@/lib/admin/procedure-types-types";

const STEPS = [
  { id: 1, title: "Nombre del trámite" },
  { id: 2, title: "Consulta vehículo" },
  { id: 3, title: "Actores" },
  { id: 4, title: "Documentos" },
] as const;

type ActorDraft = { id: string; roleLabel: string };
type DocumentDraft = { id: string; label: string; kind: DocumentKind };

type Props = {
  mode: "create" | "edit";
  procedureTypeId?: string;
  initial?: ProcedureTypeDetail;
};

function normalizeVehicleMode(mode: ProcedureTypeDetail["vehicleQueryMode"]): VehicleQueryMode {
  return mode === "Vin" || mode === 1 ? "Vin" : "Plate";
}

function normalizeDocumentKind(kind: DocumentKind | 0 | 1): DocumentKind {
  return kind === "Dynamic" || kind === 1 ? "Dynamic" : "Static";
}

export function ProcedureTypeWizard({ mode, procedureTypeId, initial }: Props) {
  const router = useRouter();
  const [step, setStep] = useState(1);
  const [name, setName] = useState(initial?.name ?? "");
  const [vehicleQueryMode, setVehicleQueryMode] = useState<VehicleQueryMode>(
    initial ? normalizeVehicleMode(initial.vehicleQueryMode) : "Plate",
  );
  const [actors, setActors] = useState<ActorDraft[]>(
    initial?.actors.map((a) => ({ id: crypto.randomUUID(), roleLabel: a.roleLabel })) ?? [],
  );
  const [documents, setDocuments] = useState<DocumentDraft[]>(
    initial?.documents.map((d) => ({
      id: crypto.randomUUID(),
      label: d.label,
      kind: normalizeDocumentKind(d.kind),
    })) ?? [],
  );
  const [newActor, setNewActor] = useState("");
  const [newDocumentLabel, setNewDocumentLabel] = useState("");
  const [newDocumentKind, setNewDocumentKind] = useState<DocumentKind>("Static");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const payload = useMemo<SaveProcedureTypePayload>(
    () => ({
      name: name.trim(),
      vehicleQueryMode,
      actors: actors.map((a) => ({ roleLabel: a.roleLabel.trim() })),
      documents: documents.map((d) => ({ label: d.label.trim(), kind: d.kind })),
    }),
    [actors, documents, name, vehicleQueryMode],
  );

  function validateStep(current: number): string | null {
    if (current === 1 && !name.trim()) return "El nombre del trámite es obligatorio.";
    if (current === 3 && actors.length < 1) return "Debe agregar al menos un actor.";
    if (current === 4) {
      for (const doc of documents) {
        if (!doc.label.trim()) return "Cada documento debe tener una etiqueta.";
      }
    }
    return null;
  }

  function goNext() {
    const message = validateStep(step);
    if (message) {
      setError(message);
      return;
    }
    setError(null);
    setStep((s) => Math.min(4, s + 1));
  }

  function goBack() {
    setError(null);
    setStep((s) => Math.max(1, s - 1));
  }

  function addActor() {
    const label = newActor.trim();
    if (!label) return;
    setActors((prev) => [...prev, { id: crypto.randomUUID(), roleLabel: label }]);
    setNewActor("");
    setError(null);
  }

  function addDocument() {
    const label = newDocumentLabel.trim();
    if (!label) return;
    setDocuments((prev) => [...prev, { id: crypto.randomUUID(), label, kind: newDocumentKind }]);
    setNewDocumentLabel("");
    setNewDocumentKind("Static");
    setError(null);
  }

  async function save() {
    for (let s = 1; s <= 4; s += 1) {
      const message = validateStep(s);
      if (message) {
        setStep(s);
        setError(message);
        return;
      }
    }

    setSaving(true);
    setError(null);

    try {
      const url =
        mode === "create"
          ? "/api/v1/admin/procedure-types"
          : `/api/v1/admin/procedure-types/${procedureTypeId}`;
      const method = mode === "create" ? "POST" : "PUT";

      const res = await fetch(url, {
        method,
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) {
        const body = (await res.json().catch(() => null)) as { error?: string } | null;
        setError(body?.error ?? "No se pudo guardar el tipo de trámite.");
        return;
      }

      router.push("/admin/procedure-types");
      router.refresh();
    } catch {
      setError("No se pudo guardar el tipo de trámite.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
      <FlitCard className="h-fit p-4">
        <ol className="space-y-3">
          {STEPS.map((item) => (
            <li
              key={item.id}
              className={[
                "rounded-flit-md px-3 py-2 text-sm",
                step === item.id
                  ? "bg-flit-blue/10 font-semibold text-flit-blue"
                  : "text-flit-text-secondary",
              ].join(" ")}
              data-testid={`wizard-step-${item.id}`}
            >
              {item.id}. {item.title}
            </li>
          ))}
        </ol>
      </FlitCard>

      <FlitCard className="p-6" data-testid="procedure-type-wizard">
        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-flit-text-brand">Nombre del trámite</h2>
            <p className="text-sm text-flit-text-secondary">
              Nombre único en todo FLIT. Ej: Traspaso, Matrícula inicial, Cambio de color.
            </p>
            <FlitInput
              label="Nombre"
              value={name}
              onChange={(e) => setName(e.target.value)}
              data-testid="procedure-name-input"
            />
          </div>
        )}

        {step === 2 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-flit-text-brand">Consulta vehículo (RUNT)</h2>
            <fieldset className="space-y-3">
              <label className="flex cursor-pointer items-center gap-3">
                <input
                  type="radio"
                  name="vehicleQueryMode"
                  checked={vehicleQueryMode === "Plate"}
                  onChange={() => setVehicleQueryMode("Plate")}
                  data-testid="vehicle-mode-plate"
                />
                <span>Consulta por Placa</span>
              </label>
              <label className="flex cursor-pointer items-center gap-3">
                <input
                  type="radio"
                  name="vehicleQueryMode"
                  checked={vehicleQueryMode === "Vin"}
                  onChange={() => setVehicleQueryMode("Vin")}
                  data-testid="vehicle-mode-vin"
                />
                <span>Consulta por VIN</span>
              </label>
            </fieldset>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-flit-text-brand">Actores</h2>
            <p className="text-sm text-flit-text-secondary">
              Roles semánticos requeridos (mínimo uno). Ej: Vendedor, Comprador, Futuro propietario.
            </p>
            <ul className="space-y-2" data-testid="actors-list">
              {actors.map((actor) => (
                <li
                  key={actor.id}
                  className="flex items-center justify-between rounded-flit-md border border-flit-border-soft px-3 py-2"
                >
                  <span>{actor.roleLabel}</span>
                  <FlitButton
                    type="button"
                    variant="ghost"
                    onClick={() => setActors((prev) => prev.filter((a) => a.id !== actor.id))}
                  >
                    Quitar
                  </FlitButton>
                </li>
              ))}
            </ul>
            <div className="flex flex-wrap items-end gap-3">
              <FlitInput
                label="Nuevo actor"
                value={newActor}
                onChange={(e) => setNewActor(e.target.value)}
                data-testid="actor-input"
              />
              <FlitButton type="button" onClick={addActor} data-testid="add-actor-button">
                Agregar actor
              </FlitButton>
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-flit-text-brand">Documentos</h2>
            <ul className="space-y-2" data-testid="documents-list">
              {documents.map((doc) => (
                <li
                  key={doc.id}
                  className="flex items-center justify-between rounded-flit-md border border-flit-border-soft px-3 py-2"
                >
                  <span>
                    {doc.label}{" "}
                    <span className="text-flit-text-secondary">
                      ({doc.kind === "Static" ? "Estático" : "Dinámico"})
                    </span>
                  </span>
                  <FlitButton
                    type="button"
                    variant="ghost"
                    onClick={() => setDocuments((prev) => prev.filter((d) => d.id !== doc.id))}
                  >
                    Quitar
                  </FlitButton>
                </li>
              ))}
            </ul>
            <div className="grid gap-3 md:grid-cols-[1fr_auto_auto] md:items-end">
              <FlitInput
                label="Etiqueta del documento"
                value={newDocumentLabel}
                onChange={(e) => setNewDocumentLabel(e.target.value)}
                data-testid="document-label-input"
              />
              <label className="text-sm text-flit-text-primary">
                Tipo
                <select
                  className="mt-1 block w-full rounded-flit-md border border-flit-border-soft px-3 py-2"
                  value={newDocumentKind}
                  onChange={(e) => setNewDocumentKind(e.target.value as DocumentKind)}
                  data-testid="document-kind-select"
                >
                  <option value="Static">Estático</option>
                  <option value="Dynamic">Dinámico</option>
                </select>
              </label>
              <FlitButton type="button" onClick={addDocument} data-testid="add-document-button">
                Agregar documento
              </FlitButton>
            </div>

            <div className="mt-6 rounded-flit-md bg-flit-bg-modal p-4 text-sm">
              <h3 className="mb-2 font-semibold text-flit-text-brand">Resumen</h3>
              <p>
                <strong>Nombre:</strong> {name.trim()}
              </p>
              <p>
                <strong>Vehículo:</strong> {vehicleQueryModeLabel(vehicleQueryMode)}
              </p>
              <p>
                <strong>Actores:</strong> {actors.map((a) => a.roleLabel).join(", ") || "—"}
              </p>
              <p>
                <strong>Documentos:</strong>{" "}
                {documents.length === 0
                  ? "Ninguno"
                  : documents.map((d) => `${d.label} (${d.kind})`).join(", ")}
              </p>
            </div>
          </div>
        )}

        {error ? (
          <p className="mt-4 text-sm text-flit-danger" role="alert">
            {error}
          </p>
        ) : null}

        <div className="mt-6 flex flex-wrap gap-3">
          {step > 1 ? (
            <FlitButton type="button" variant="ghost" onClick={goBack}>
              Anterior
            </FlitButton>
          ) : null}
          {step < 4 ? (
            <FlitButton type="button" onClick={goNext} data-testid="wizard-next">
              Siguiente
            </FlitButton>
          ) : (
            <FlitButton type="button" onClick={save} disabled={saving} data-testid="wizard-save">
              {saving ? "Guardando…" : mode === "create" ? "Crear trámite" : "Guardar cambios"}
            </FlitButton>
          )}
        </div>
      </FlitCard>
    </div>
  );
}
