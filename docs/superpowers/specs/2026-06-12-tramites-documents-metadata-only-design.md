# Trámites — Documentos metadata-only (fix 413)

**Date:** 2026-06-12  
**Status:** Approved  
**Project:** FLIT 2.0 — FLIT EVOLUTION  
**Depends on:** [Trámites Runtime MVP A](./2026-06-12-tramites-runtime-mvp-design.md)  
**Motivation:** Confirmar trámite falla con HTTP 413 al subir PDFs vía `POST /api/v1/tramites/{id}/documents` (multipart a través del proxy Next.js → gateway). Para MVP no se requiere persistir binarios; basta registrar metadatos del documento adjunto en el wizard.

## Summary

Change document registration from **multipart PDF upload** to **JSON metadata embedded in `POST /api/v1/tramites`**. The wizard keeps the PDF file picker for client-side UX validation (type, size). The backend persists rows in `procedure_instance_documents` with `storagePath = ""` and does not store file bytes.

## Locked decisions

| Area | Decision |
|------|----------|
| Persistence | Metadata only — no PDF binary, no filesystem write on create |
| Transport | Single atomic `POST /api/v1/tramites` with `documents[]` in JSON body |
| UX | User still selects PDF in wizard step 5; file never sent over HTTP |
| `storagePath` | Empty string `""` means no stored file |
| Max size | 10 MB per document (`Procedures:MaxDocumentSizeBytes`, default 10485760) |
| Server PDF validation | Filename + size only; no magic-byte check (no binary available) |
| Multipart endpoint | Keep `POST /{id}/documents` in codebase for future file storage; wizard does not call it |
| Schema migration | None — existing `procedure_instance_documents` columns suffice |

## Problem

Current flow on confirm:

1. `POST /api/v1/tramites` — create instance  
2. For each document: `POST /api/v1/tramites/{id}/documents?label=…` with `multipart/form-data` and PDF bytes  

Requests hit `dev.davidch.flitsas.online/api/…` → Next.js rewrite → YARP gateway → core-api. Intermediate proxies reject large bodies (413 Payload Too Large) before the API handler runs.

## Chosen approach (vs alternatives)

| Approach | Verdict |
|----------|---------|
| **A — Metadata in create request (chosen)** | One JSON request, atomic, no 413, simplest FE |
| B — Separate JSON `POST /documents` after create | N+1 calls, partial failure risk |
| C — Feature flag dual mode (JSON / multipart) | Over-engineering for current need |
| Increase proxy body limits only | Band-aid; still sends large payloads unnecessarily |

## Architecture

```
Usuario → Wizard (paso 5: selecciona PDF)
       → Validación cliente (tipo PDF, tamaño ≤ 10 MB)
       → Confirmar → POST /api/v1/tramites (JSON + documents metadata)
       → TramitesCreateHandler: valida + INSERT instance + document rows (una transacción)
       → storagePath = ""
```

The existing multipart upload handler and `ProcedureDocumentStorage` remain for a future phase when binary persistence is required.

## API changes

### Extended create request

`POST /api/v1/tramites` — add optional `documents` array:

```json
{
  "procedureTypeId": "uuid",
  "otDivipolCode": "11001000",
  "vehicleQueryValue": "ABC123",
  "actors": [ … ],
  "documents": [
    {
      "label": "IMPRONTA",
      "fileName": "impronta.pdf",
      "fileSizeBytes": 2450000
    }
  ]
}
```

### New DTOs

```csharp
public sealed record CreateTramiteDocumentRequest(
    string Label,
    string FileName,
    long FileSizeBytes);
```

Extend `CreateTramiteRequest` with `IReadOnlyList<CreateTramiteDocumentRequest> Documents`.

### Backend validation (`TramitesCreateHandler`)

After existing actor/OT/type checks:

1. Load static document labels from procedure definition (`DocumentKind.Static`).
2. If definition has static documents:
   - `documents` must be present and contain exactly one entry per required label (case-sensitive match on `label`).
3. If definition has no static documents:
   - `documents` must be empty or omitted.
4. Per document entry:
   - `fileName` non-empty, ends with `.pdf` (case-insensitive).
   - `fileSizeBytes` > 0 and ≤ `Procedures:MaxDocumentSizeBytes` (default 10 MB).
   - `label` must match a static document in the definition.
5. Insert `ProcedureInstanceDocument` rows in the same `SaveChanges` as the instance:
   - `Kind = Static`, `FileName`, `FileSizeBytes`, `UploadedAt = UtcNow`, `StoragePath = ""`.

On any validation failure: return `400 VALIDATION_ERROR` with existing error shape; no instance persisted.

### Multipart endpoint (unchanged, deferred)

`POST /api/v1/tramites/{id:guid}/documents?label={label}` — multipart PDF upload remains implemented but **out of wizard scope**. Future work: populate `storagePath` via this endpoint or object storage.

## Data model

No migration. Use existing table:

| Column | MVP value |
|--------|-----------|
| `label` | From request |
| `kind` | `Static` |
| `file_name` | From request |
| `file_size_bytes` | From request |
| `uploaded_at` | Server timestamp at create |
| `storage_path` | `""` (contract: empty = no binary) |

Unique index `(procedure_instance_id, label)` prevents duplicates.

## Frontend changes

| File | Change |
|------|--------|
| `frontend/lib/tramites/client-api.ts` | `createTramite()` sends `documents` metadata; stop calling `uploadTramiteDocument` from wizard |
| `frontend/components/tramites/ProcedureInstanceWizard.tsx` | `submit()` single create call; step 5 validation adds max size check |
| `uploadTramiteDocument()` | Keep function unused or document as reserved for future storage phase |

Step 5 validation (client):

- File required per static document label.
- `isPdfFile()` (existing).
- `file.size <= 10 * 1024 * 1024`.

Submit payload example:

```typescript
documents: documents
  .filter((d) => d.file)
  .map((d) => ({
    label: d.label,
    fileName: d.file!.name,
    fileSizeBytes: d.file!.size,
  }))
```

## Error messages

| Case | HTTP | Message (examples) |
|------|------|-------------------|
| Missing required label | 400 | `Falta el documento requerido: {label}.` |
| Extra or unknown label | 400 | `Document label is not a static document in this procedure type.` |
| Invalid fileName | 400 | `fileName must be a PDF file name.` |
| Size out of range | 400 | `fileSizeBytes exceeds maximum allowed (10 MB).` |
| Count mismatch | 400 | `Document count does not match procedure definition.` |

## Testing

### Backend integration (`TramitesCreateTests` and new cases)

- Create with valid `documents` metadata → 201, DB rows with empty `storagePath`.
- Missing document for type with static docs → 400.
- Unknown label → 400.
- `fileSizeBytes` over max → 400.
- Type without static docs, empty `documents` → 201.

Update existing create tests: procedure types seeded with static documents must include matching `documents` in POST body.

### Existing upload tests

`TramitesDocumentUploadTests` — keep as-is; covers future multipart path, not wizard flow.

### E2E

`frontend/e2e/tramites/tramites-create.spec.ts` — already waits for single `POST /api/v1/tramites`; should pass after payload includes metadata (no second upload requests).

## Future migration to file storage

When PDF persistence is required:

1. Use `POST /{id}/documents` multipart or presigned URL flow.
2. On success, set `storagePath` (and optionally re-validate PDF magic bytes server-side).
3. Legacy rows with `storagePath == ""` may require re-upload before OT send.

No feature flag in this phase.

## Non-goals

- Persisting PDF binaries or writing to `ProcedureDocumentStorage` on create
- Chunked / resumable / presigned uploads
- Server-side PDF content validation without binary
- Raising nginx or Next.js proxy body limits as the primary fix
- Removing multipart upload endpoint or storage infrastructure

## Supersedes (partial)

Overrides the document upload section of [Trámites Runtime MVP A](./2026-06-12-tramites-runtime-mvp-design.md):

- **Was:** Separate multipart upload per label after create; local filesystem storage on upload.
- **Now (interim):** Metadata in create request; `storagePath` empty; multipart endpoint dormant for wizard.

Revert or extend this spec when enabling real file storage.
