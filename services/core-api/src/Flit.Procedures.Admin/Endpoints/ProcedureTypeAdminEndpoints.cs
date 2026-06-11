using Flit.Identity.Shared.Auth;
using Flit.Procedures.Admin.Auth;
using Flit.Procedures.Admin.Crud;
using Flit.Procedures.Admin.Index;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Procedures.Admin.Endpoints;

public static class ProcedureTypeAdminEndpoints
{
    public static IEndpointRouteBuilder MapProcedureTypeAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/procedure-types")
            .RequireSuperAdmin();

        group.MapGet("/index", GetIndexAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPatch("/{id:guid}/status", UpdateStatusAsync);

        return app;
    }

    private static Task<IResult> GetIndexAsync(
        ProcedureTypeIndexHandler handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? name = null,
        bool? isActive = null) =>
        handler.HandleAsync(
            new ProcedureTypeIndexRequest(page, pageSize, sort, name, isActive),
            ct);

    private static Task<IResult> CreateAsync(
        SaveProcedureTypeRequest request,
        HttpContext http,
        ProcedureTypeCrudHandler handler,
        CancellationToken ct)
    {
        var user = http.Items["CurrentUser"] as CurrentUser;
        return handler.CreateAsync(request, user?.Id, ct);
    }

    private static Task<IResult> GetDetailAsync(
        Guid id,
        ProcedureTypeCrudHandler handler,
        CancellationToken ct) =>
        handler.GetDetailAsync(id, ct);

    private static Task<IResult> UpdateAsync(
        Guid id,
        SaveProcedureTypeRequest request,
        HttpContext http,
        ProcedureTypeCrudHandler handler,
        CancellationToken ct)
    {
        var user = http.Items["CurrentUser"] as CurrentUser;
        return handler.UpdateAsync(id, request, user?.Id, ct);
    }

    private static Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateProcedureTypeStatusRequest request,
        HttpContext http,
        ProcedureTypeCrudHandler handler,
        CancellationToken ct)
    {
        var user = http.Items["CurrentUser"] as CurrentUser;
        return handler.UpdateStatusAsync(id, request, user?.Id, ct);
    }
}
