using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Runtime.Ot;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Create;

public sealed class TramitesCreateHandler(
    ProceduresDbContext db,
    IProcedureDefinitionService definitions,
    TrafficAuthorityPicker trafficAuthorityPicker)
{
    public async Task<IResult> HandleAsync(
        CreateTramiteRequest request,
        CurrentUser user,
        CancellationToken ct)
    {
        if (user.TenantId is not { } tenantId)
        {
            return Forbidden(
                "Los trámites requieren un usuario con tenant asignado. Inicie sesión como administrador de compañía.");
        }

        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var definition = await definitions.GetByIdAsync(request.ProcedureTypeId, ct);
        if (definition is null)
        {
            return NotFound("Procedure type not found or inactive.");
        }

        var ot = await trafficAuthorityPicker.ResolveAsync(tenantId, request.OtDivipolCode.Trim(), ct);
        if (ot is null)
        {
            return ValidationError("Invalid or unavailable traffic authority.");
        }

        var actorValidation = ValidateActors(request.Actors, definition);
        if (actorValidation is not null)
        {
            return actorValidation;
        }

        var now = DateTimeOffset.UtcNow;
        var instance = new ProcedureInstance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProcedureTypeId = definition.Id,
            ProcedureTypeCode = definition.Code,
            OtTenantId = ot.OtTenantId,
            OtDivipolCode = ot.DivipolCode,
            VehicleQueryValue = request.VehicleQueryValue.Trim(),
            Status = ProcedureStatus.PendienteEnvio,
            CreatedBy = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var actorRequest in request.Actors.OrderBy(a => a.SortOrder))
        {
            var actor = new ProcedureInstanceActor
            {
                Id = Guid.NewGuid(),
                ProcedureInstanceId = instance.Id,
                RoleLabel = actorRequest.RoleLabel.Trim(),
                SortOrder = actorRequest.SortOrder,
                PersonKind = actorRequest.PersonKind,
                DocumentType = actorRequest.DocumentType,
                DocumentNumber = actorRequest.DocumentNumber.Trim(),
                IsLegalRepresentative = false,
            };
            instance.Actors.Add(actor);

            if (actorRequest.PersonKind == PersonKind.Juridica)
            {
                var rep = actorRequest.LegalRepresentative!;
                instance.Actors.Add(new ProcedureInstanceActor
                {
                    Id = Guid.NewGuid(),
                    ProcedureInstanceId = instance.Id,
                    RoleLabel = "Representante Legal",
                    SortOrder = actorRequest.SortOrder,
                    PersonKind = PersonKind.Natural,
                    DocumentType = rep.DocumentType,
                    DocumentNumber = rep.DocumentNumber.Trim(),
                    IsLegalRepresentative = true,
                    ParentActorId = actor.Id,
                });
            }
        }

        db.ProcedureInstances.Add(instance);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/tramites/{instance.Id}",
            new TramiteCreatedResponse(instance.Id, instance.Status.ToString()));
    }

    private static IResult? ValidateRequest(CreateTramiteRequest request)
    {
        if (request.ProcedureTypeId == Guid.Empty)
        {
            return ValidationError("procedureTypeId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OtDivipolCode))
        {
            return ValidationError("otDivipolCode is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VehicleQueryValue))
        {
            return ValidationError("vehicleQueryValue is required.");
        }

        if (request.Actors.Count == 0)
        {
            return ValidationError("At least one actor is required.");
        }

        return null;
    }

    private static IResult? ValidateActors(
        IReadOnlyList<CreateTramiteActorRequest> actors,
        ProcedureDefinitionDto definition)
    {
        var expected = definition.Actors.OrderBy(a => a.SortOrder).ToList();
        if (actors.Count != expected.Count)
        {
            return ValidationError("Actor count does not match procedure definition.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            var actual = actors.OrderBy(a => a.SortOrder).ElementAt(i);
            var exp = expected[i];
            if (!string.Equals(actual.RoleLabel.Trim(), exp.RoleLabel, StringComparison.Ordinal)
                || actual.SortOrder != exp.SortOrder)
            {
                return ValidationError("Actor roles do not match procedure definition.");
            }

            if (actual.PersonKind == PersonKind.Juridica)
            {
                if (actual.DocumentType != DocumentIdType.Nit)
                {
                    return ValidationError("Juridical actors must use NIT document type.");
                }

                if (actual.LegalRepresentative is null
                    || string.IsNullOrWhiteSpace(actual.LegalRepresentative.DocumentNumber))
                {
                    return ValidationError("Legal representative is required for juridical actors.");
                }
            }
            else if (actual.LegalRepresentative is not null)
            {
                return ValidationError("Legal representative is only allowed for juridical actors.");
            }
        }

        return null;
    }

    private static IResult NotFound(string message) =>
        Results.Json(new { code = ApiErrorCodes.NotFound, message }, statusCode: StatusCodes.Status404NotFound);

    private static IResult Forbidden(string? message = null) =>
        Results.Json(
            new { code = ApiErrorCodes.Forbidden, message },
            statusCode: StatusCodes.Status403Forbidden);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = ApiErrorCodes.ValidationError, message }, statusCode: StatusCodes.Status400BadRequest);
}
