using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Documents;

public sealed class TramitesDocumentUploadHandler(
    ProceduresDbContext db,
    IProcedureDefinitionService definitions,
    ProcedureDocumentStorage storage)
{
    public async Task<IResult> HandleAsync(
        Guid instanceId,
        string label,
        IFormFile file,
        CurrentUser user,
        CancellationToken ct)
    {
        if (user.TenantId is not { } tenantId)
        {
            return Forbidden();
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return ValidationError("label is required.");
        }

        if (file.Length == 0)
        {
            return ValidationError("file is required.");
        }

        if (!await IsPdfAsync(file, ct))
        {
            return ValidationError("Only PDF files are allowed.");
        }

        var instance = await db.ProcedureInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.TenantId == tenantId, ct);

        if (instance is null)
        {
            return NotFound("Procedure instance not found.");
        }

        var definition = await definitions.GetByIdAsync(instance.ProcedureTypeId, ct);
        if (definition is null)
        {
            return NotFound("Procedure type not found or inactive.");
        }

        var documentDef = definition.Documents
            .FirstOrDefault(d =>
                string.Equals(d.Label, label.Trim(), StringComparison.Ordinal)
                && d.Kind == DocumentKind.Static);

        if (documentDef is null)
        {
            return ValidationError("Document label is not a static document in this procedure type.");
        }

        await using var stream = file.OpenReadStream();
        var stored = await storage.SaveAsync(
            tenantId,
            instanceId,
            documentDef.Label,
            stream,
            file.FileName,
            ct);

        var now = DateTimeOffset.UtcNow;
        var existing = await db.ProcedureInstanceDocuments
            .FirstOrDefaultAsync(
                d => d.ProcedureInstanceId == instanceId && d.Label == documentDef.Label,
                ct);

        if (existing is null)
        {
            existing = new ProcedureInstanceDocument
            {
                Id = Guid.NewGuid(),
                ProcedureInstanceId = instanceId,
                Label = documentDef.Label,
                Kind = DocumentKind.Static,
            };
            db.ProcedureInstanceDocuments.Add(existing);
        }

        existing.StoragePath = stored.StoragePath;
        existing.FileName = Path.GetFileName(file.FileName);
        existing.FileSizeBytes = stored.FileSizeBytes;
        existing.UploadedAt = now;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new DocumentUploadedResponse(
            existing.Id,
            existing.Label,
            existing.FileName,
            existing.FileSizeBytes,
            existing.UploadedAt));
    }

    private static async Task<bool> IsPdfAsync(IFormFile file, CancellationToken ct)
    {
        var contentType = file.ContentType;
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await HasPdfMagicBytesAsync(file, ct);
        }

        return false;
    }

    private static async Task<bool> HasPdfMagicBytesAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var buffer = new byte[4];
        var read = await stream.ReadAsync(buffer.AsMemory(0, 4), ct);
        if (read < 4)
        {
            return false;
        }

        return buffer[0] == (byte)'%' && buffer[1] == (byte)'P' && buffer[2] == (byte)'D' && buffer[3] == (byte)'F';
    }

    private static IResult NotFound(string message) =>
        Results.Json(new { code = ApiErrorCodes.NotFound, message }, statusCode: StatusCodes.Status404NotFound);

    private static IResult Forbidden() =>
        Results.Json(new { code = ApiErrorCodes.Forbidden }, statusCode: StatusCodes.Status403Forbidden);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}

public sealed record DocumentUploadedResponse(
    Guid Id,
    string Label,
    string FileName,
    long FileSizeBytes,
    DateTimeOffset UploadedAt);
