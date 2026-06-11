using Flit.OT.Admin.Auth;
using Flit.OT.Admin.Config;
using Flit.OT.Admin.Crud;
using Flit.OT.Admin.Index;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.OT.Admin.Endpoints;

public static class OtAdminEndpoints
{
    public static IEndpointRouteBuilder MapOtAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/ot")
            .RequireSuperAdmin();

        group.MapGet("/index", GetIndexAsync);
        group.MapPost("/", CreateOtAsync);
        group.MapGet("/{id:guid}", GetOtAsync);
        group.MapPatch("/{id:guid}", UpdateOtAsync);
        group.MapPatch("/{id:guid}/status", UpdateOtStatusAsync);

        group.MapGet("/{id:guid}/config/integration", GetIntegrationAsync);
        group.MapPut("/{id:guid}/config/integration", PutIntegrationAsync);

        return app;
    }

    private static Task<IResult> GetIntegrationAsync(
        Guid id,
        OtIntegrationHandler handler,
        CancellationToken ct) =>
        handler.GetForProfileAsync(id, ct);

    private static Task<IResult> PutIntegrationAsync(
        Guid id,
        PutIntegrationRequest request,
        OtIntegrationHandler handler,
        CancellationToken ct) =>
        handler.PutForProfileAsync(id, request, ct);

    private static Task<IResult> CreateOtAsync(
        CreateOtRequest request,
        OtCrudHandler handler,
        CancellationToken ct) =>
        handler.CreateAsync(request, ct);

    private static Task<IResult> GetOtAsync(
        Guid id,
        OtCrudHandler handler,
        CancellationToken ct) =>
        handler.GetDetailAsync(id, ct);

    private static Task<IResult> UpdateOtAsync(
        Guid id,
        UpdateOtRequest request,
        OtCrudHandler handler,
        CancellationToken ct) =>
        handler.UpdateAsync(id, request, ct);

    private static Task<IResult> UpdateOtStatusAsync(
        Guid id,
        UpdateOtStatusRequest request,
        OtCrudHandler handler,
        CancellationToken ct) =>
        handler.UpdateStatusAsync(id, request, ct);

    private static Task<IResult> GetIndexAsync(
        OtIndexHandler handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? divipol = null,
        string? name = null,
        Guid? id = null,
        DateTimeOffset? auditFrom = null,
        DateTimeOffset? auditTo = null) =>
        handler.HandleAsync(new OtIndexRequest
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Divipol = divipol,
            Name = name,
            Id = id,
            AuditFrom = auditFrom,
            AuditTo = auditTo
        }, ct);
}
