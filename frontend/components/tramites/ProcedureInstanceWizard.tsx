"use client";

import { useEffect, useMemo, useState } from "react";
import { FlitButton } from "@/components/flit/Button";
import { FlitCard } from "@/components/flit/Card";
import { FlitInput } from "@/components/flit/Input";
import { vehicleQueryModeLabel } from "@/lib/admin/procedure-types-api";
import {
  createTramite,
  fetchProcedureDefinition,
  fetchProcedureTypes,
  fetchTrafficAuthorities,
  isVinMode,
  lookupRues,
  lookupSimit,
  queryRunt,
  staticDocuments,
  uploadTramiteDocument,
} from "@/lib/tramites/client-api";
import type {
  ActorFormState,
  DocumentFileState,
  DocumentIdType,
  PersonKind,
  ProcedureDefinition,
  ProcedureTypeSummary,
  TrafficAuthority,
} from "@/lib/tramites/types";

const STEPS = [
  { id: 1, title: "Tipo de trámite" },
  { id: 2, title: "Organismo de tránsito" },
  { id: 3, title: "Vehículo" },
  { id: 4, title: "Actores" },
  { id: 5, title: "Documentos" },
  { id: 6, title: "Confirmar" },
] as const;

type Props = {
  onClose: () => void;
  onSuccess: () => void;
};

function initialActors(definition: ProcedureDefinition | null): ActorFormState[] {
  if (!definition) return [];
  return definition.actors.map((actor) => ({
    roleLabel: actor.roleLabel,
    sortOrder: actor.sortOrder,
    personKind: "Natural",
    documentType: "Cc",
    documentNumber: "",
  }));
}

function isPdfFile(file: File): boolean {
  return file.type === "application/pdf" || file.name.toLowerCase().endsWith(".pdf");
}

export function ProcedureInstanceWizard({ onClose, onSuccess }: Props) {
  const [step, setStep] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [lookupPreview, setLookupPreview] = useState<string | null>(null);

  const [procedureTypes, setProcedureTypes] = useState<ProcedureTypeSummary[]>([]);
  const [trafficAuthorities, setTrafficAuthorities] = useState<TrafficAuthority[]>([]);
  const [definition, setDefinition] = useState<ProcedureDefinition | null>(null);

  const [selectedTypeId, setSelectedTypeId] = useState("");
  const [otDivipolCode, setOtDivipolCode] = useState("");
  const [vehicleQueryValue, setVehicleQueryValue] = useState("");
  const [actors, setActors] = useState<ActorFormState[]>([]);
  const [documents, setDocuments] = useState<DocumentFileState[]>([]);

  useEffect(() => {
    void fetchProcedureTypes()
      .then(setProcedureTypes)
      .catch((e: Error) => setError(e.message));
    void fetchTrafficAuthorities()
      .then(setTrafficAuthorities)
      .catch((e: Error) => setError(e.message));
  }, []);

  function handleTypeChange(typeId: string) {
    setSelectedTypeId(typeId);
    if (!typeId) {
      setDefinition(null);
      setActors([]);
      setDocuments([]);
      return;
    }

    void fetchProcedureDefinition(typeId)
      .then((def) => {
        setDefinition(def);
        setActors(initialActors(def));
        setDocuments(staticDocuments(def));
      })
      .catch((e: Error) => setError(e.message));
  }

  const selectedOt = useMemo(
    () => trafficAuthorities.find((o) => o.divipolCode === otDivipolCode) ?? null,
    [otDivipolCode, trafficAuthorities],
  );

  function validateStep(current: number): string | null {
    if (current === 1 && !selectedTypeId) return "Seleccione un tipo de trámite.";
    if (current === 2 && !otDivipolCode) return "Seleccione un organismo de tránsito.";
    if (current === 3 && !vehicleQueryValue.trim()) {
      return definition && isVinMode(definition.vehicleQueryMode)
        ? "Ingrese el VIN del vehículo."
        : "Ingrese la placa del vehículo.";
    }
    if (current === 4) {
      for (const actor of actors) {
        if (!actor.documentNumber.trim()) {
          return `Complete el documento del actor ${actor.roleLabel}.`;
        }
        if (actor.personKind === "Juridica") {
          if (actor.documentType !== "Nit") {
            return `El actor ${actor.roleLabel} jurídico debe usar NIT.`;
          }
          if (!actor.legalRepresentative?.documentNumber.trim()) {
            return `El representante legal de ${actor.roleLabel} es obligatorio.`;
          }
        }
      }
    }
    if (current === 5) {
      for (const doc of documents) {
        if (!doc.file) return `Adjunte el PDF para ${doc.label}.`;
        if (!isPdfFile(doc.file)) return `Solo se permiten archivos PDF (${doc.label}).`;
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
    setLookupPreview(null);
    setStep((s) => Math.min(6, s + 1));
  }

  function goBack() {
    setError(null);
    setLookupPreview(null);
    setStep((s) => Math.max(1, s - 1));
  }

  function updateActor(index: number, patch: Partial<ActorFormState>) {
    setActors((prev) =>
      prev.map((actor, i) => {
        if (i !== index) return actor;
        const next = { ...actor, ...patch };
        if (patch.personKind === "Juridica") {
          next.documentType = "Nit";
          next.legalRepresentative = next.legalRepresentative ?? {
            documentType: "Cc",
            documentNumber: "",
          };
        } else if (patch.personKind === "Natural") {
          next.legalRepresentative = undefined;
        }
        return next;
      }),
    );
  }

  async function consultVehicle() {
    if (!definition || !vehicleQueryValue.trim()) return;
    try {
      const type = isVinMode(definition.vehicleQueryMode) ? "vin" : "placa";
      const data = await queryRunt(type, vehicleQueryValue.trim());
      setLookupPreview(JSON.stringify(data, null, 2));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Consulta RUNT fallida.");
    }
  }

  async function consultActor(index: number) {
    const actor = actors[index];
    if (!actor.documentNumber.trim()) return;
    try {
      const data =
        actor.personKind === "Juridica"
          ? await lookupRues(actor.documentNumber.trim())
          : await lookupSimit(actor.documentType, actor.documentNumber.trim());
      setLookupPreview(JSON.stringify(data, null, 2));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Consulta fallida.");
    }
  }

  async function submit() {
    for (let s = 1; s <= 5; s += 1) {
      const message = validateStep(s);
      if (message) {
        setStep(s);
        setError(message);
        return;
      }
    }

    if (!definition) return;

    setSubmitting(true);
    setError(null);
    try {
      const created = await createTramite({
        procedureTypeId: selectedTypeId,
        otDivipolCode,
        vehicleQueryValue: vehicleQueryValue.trim(),
        actors,
      });

      for (const doc of documents) {
        if (doc.file) {
          await uploadTramiteDocument(created.id, doc.label, doc.file);
        }
      }

      onSuccess();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo crear el trámite.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="tramite-wizard-title"
      data-testid="tramite-wizard"
    >
      <FlitCard className="max-h-[90vh] w-full max-w-3xl overflow-y-auto p-6">
        <div className="mb-6 flex items-start justify-between gap-4">
          <div>
            <h2 id="tramite-wizard-title" className="text-xl font-bold text-flit-text-brand">
              Nuevo trámite
            </h2>
            <p className="mt-1 text-sm text-flit-text-secondary">
              Paso {step} de {STEPS.length}: {STEPS[step - 1]?.title}
            </p>
          </div>
          <FlitButton type="button" variant="ghost" onClick={onClose}>
            Cerrar
          </FlitButton>
        </div>

        <ol className="mb-6 flex flex-wrap gap-2">
          {STEPS.map((item) => (
            <li
              key={item.id}
              className={[
                "rounded-flit-pill px-3 py-1 text-xs font-semibold",
                item.id === step
                  ? "bg-flit-blue text-flit-text-inverse"
                  : "bg-flit-bg-modal text-flit-text-secondary",
              ].join(" ")}
              data-testid={`wizard-step-${item.id}`}
            >
              {item.id}. {item.title}
            </li>
          ))}
        </ol>

        {step === 1 && (
          <div className="space-y-4">
            <label className="block text-sm font-semibold text-flit-text-primary">
              Tipo de trámite
              <select
                className="mt-2 w-full rounded-flit-md border border-flit-border-soft bg-flit-bg-card px-3 py-2 text-sm"
                value={selectedTypeId}
                onChange={(e) => handleTypeChange(e.target.value)}
                data-testid="procedure-type-select"
              >
                <option value="">Seleccione…</option>
                {procedureTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name}
                  </option>
                ))}
              </select>
            </label>
            {definition && (
              <p className="text-sm text-flit-text-secondary">
                Consulta vehículo: {vehicleQueryModeLabel(definition.vehicleQueryMode)}
              </p>
            )}
          </div>
        )}

        {step === 2 && (
          <fieldset className="space-y-3">
            <legend className="text-sm font-semibold text-flit-text-primary">
              Organismo de tránsito
            </legend>
            {trafficAuthorities.length === 0 && (
              <p className="text-sm text-flit-text-secondary">
                No hay organismos de tránsito disponibles para su compañía.
              </p>
            )}
            {trafficAuthorities.map((ot) => (
              <label
                key={ot.divipolCode}
                className="flex cursor-pointer items-center gap-3 rounded-flit-md border border-flit-border-soft px-4 py-3"
                data-testid={`ot-option-${ot.divipolCode}`}
              >
                <input
                  type="radio"
                  name="ot"
                  value={ot.divipolCode}
                  checked={otDivipolCode === ot.divipolCode}
                  onChange={() => setOtDivipolCode(ot.divipolCode)}
                />
                <span className="text-sm text-flit-text-primary">{ot.displayName}</span>
              </label>
            ))}
          </fieldset>
        )}

        {step === 3 && definition && (
          <div className="space-y-4">
            <FlitInput
              label={isVinMode(definition.vehicleQueryMode) ? "VIN" : "Placa"}
              value={vehicleQueryValue}
              onChange={(e) => setVehicleQueryValue(e.target.value)}
              data-testid="vehicle-query-input"
            />
            <FlitButton type="button" variant="ghost" onClick={() => void consultVehicle()}>
              Consultar RUNT
            </FlitButton>
          </div>
        )}

        {step === 4 && (
          <div className="space-y-6">
            {actors.map((actor, index) => (
              <div
                key={`${actor.roleLabel}-${actor.sortOrder}`}
                className="rounded-flit-md border border-flit-border-soft p-4"
                data-testid={`actor-form-${actor.sortOrder}`}
              >
                <h3 className="mb-3 text-sm font-semibold text-flit-text-brand">
                  {actor.roleLabel}
                </h3>
                <div className="grid gap-3 sm:grid-cols-2">
                  <label className="text-sm">
                    Tipo de persona
                    <select
                      className="mt-1 w-full rounded-flit-md border border-flit-border-soft px-3 py-2"
                      value={actor.personKind}
                      onChange={(e) =>
                        updateActor(index, { personKind: e.target.value as PersonKind })
                      }
                      data-testid={`actor-kind-${actor.sortOrder}`}
                    >
                      <option value="Natural">Natural</option>
                      <option value="Juridica">Jurídica</option>
                    </select>
                  </label>
                  <label className="text-sm">
                    Tipo documento
                    <select
                      className="mt-1 w-full rounded-flit-md border border-flit-border-soft px-3 py-2"
                      value={actor.documentType}
                      disabled={actor.personKind === "Juridica"}
                      onChange={(e) =>
                        updateActor(index, { documentType: e.target.value as DocumentIdType })
                      }
                      data-testid={`actor-doc-type-${actor.sortOrder}`}
                    >
                      <option value="Cc">CC</option>
                      <option value="Ce">CE</option>
                      <option value="Passport">Pasaporte</option>
                      {actor.personKind === "Juridica" && <option value="Nit">NIT</option>}
                    </select>
                  </label>
                  <div className="sm:col-span-2">
                    <FlitInput
                      label="Número de documento"
                      value={actor.documentNumber}
                      onChange={(e) => updateActor(index, { documentNumber: e.target.value })}
                      data-testid={`actor-doc-number-${actor.sortOrder}`}
                    />
                  </div>
                </div>
                {actor.personKind === "Juridica" && (
                  <div className="mt-4 rounded-flit-md bg-flit-bg-modal p-3">
                    <p className="mb-2 text-xs font-semibold uppercase text-flit-text-secondary">
                      Representante legal
                    </p>
                    <div className="grid gap-3 sm:grid-cols-2">
                      <label className="text-sm">
                        Tipo documento
                        <select
                          className="mt-1 w-full rounded-flit-md border border-flit-border-soft px-3 py-2"
                          value={actor.legalRepresentative?.documentType ?? "Cc"}
                          onChange={(e) =>
                            updateActor(index, {
                              legalRepresentative: {
                                documentType: e.target.value as DocumentIdType,
                                documentNumber: actor.legalRepresentative?.documentNumber ?? "",
                              },
                            })
                          }
                        >
                          <option value="Cc">CC</option>
                          <option value="Ce">CE</option>
                          <option value="Passport">Pasaporte</option>
                        </select>
                      </label>
                      <FlitInput
                        label="Número"
                        value={actor.legalRepresentative?.documentNumber ?? ""}
                        onChange={(e) =>
                          updateActor(index, {
                            legalRepresentative: {
                              documentType: actor.legalRepresentative?.documentType ?? "Cc",
                              documentNumber: e.target.value,
                            },
                          })
                        }
                        data-testid={`actor-legal-rep-${actor.sortOrder}`}
                      />
                    </div>
                  </div>
                )}
                <FlitButton
                  type="button"
                  variant="ghost"
                  className="mt-3"
                  onClick={() => void consultActor(index)}
                >
                  Consultar
                </FlitButton>
              </div>
            ))}
          </div>
        )}

        {step === 5 && (
          <div className="space-y-4">
            {documents.map((doc, index) => (
              <label
                key={doc.label}
                className="block text-sm font-semibold text-flit-text-primary"
                data-testid={`document-upload-${index}`}
              >
                {doc.label} (PDF)
                <input
                  type="file"
                  accept="application/pdf,.pdf"
                  className="mt-2 block w-full text-sm"
                  onChange={(e) => {
                    const file = e.target.files?.[0] ?? null;
                    setDocuments((prev) =>
                      prev.map((item, i) => (i === index ? { ...item, file } : item)),
                    );
                  }}
                />
              </label>
            ))}
            {documents.length === 0 && (
              <p className="text-sm text-flit-text-secondary">
                Este trámite no requiere documentos estáticos.
              </p>
            )}
          </div>
        )}

        {step === 6 && definition && (
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
            <div>
              <dt className="font-semibold text-flit-text-secondary">Tipo</dt>
              <dd>{definition.name}</dd>
            </div>
            <div>
              <dt className="font-semibold text-flit-text-secondary">OT</dt>
              <dd>{selectedOt?.displayName ?? otDivipolCode}</dd>
            </div>
            <div>
              <dt className="font-semibold text-flit-text-secondary">Vehículo</dt>
              <dd>{vehicleQueryValue}</dd>
            </div>
            <div>
              <dt className="font-semibold text-flit-text-secondary">Actores</dt>
              <dd>{actors.length}</dd>
            </div>
            <div>
              <dt className="font-semibold text-flit-text-secondary">Documentos</dt>
              <dd>{documents.filter((d) => d.file).length}</dd>
            </div>
            <div>
              <dt className="font-semibold text-flit-text-secondary">Estado final</dt>
              <dd>Pendiente de envío</dd>
            </div>
          </dl>
        )}

        {lookupPreview && (
          <pre className="mt-4 max-h-40 overflow-auto rounded-flit-md bg-flit-bg-modal p-3 text-xs">
            {lookupPreview}
          </pre>
        )}

        {error && (
          <p className="mt-4 text-sm text-flit-danger" role="alert">
            {error}
          </p>
        )}

        <div className="mt-8 flex flex-wrap justify-between gap-3">
          <FlitButton
            type="button"
            variant="ghost"
            onClick={goBack}
            disabled={step === 1 || submitting}
            data-testid="wizard-back"
          >
            Atrás
          </FlitButton>
          {step < 6 ? (
            <FlitButton type="button" onClick={goNext} data-testid="wizard-next">
              Siguiente
            </FlitButton>
          ) : (
            <FlitButton
              type="button"
              onClick={() => void submit()}
              disabled={submitting}
              data-testid="wizard-submit"
            >
              {submitting ? "Guardando…" : "Confirmar trámite"}
            </FlitButton>
          )}
        </div>
      </FlitCard>
    </div>
  );
}
