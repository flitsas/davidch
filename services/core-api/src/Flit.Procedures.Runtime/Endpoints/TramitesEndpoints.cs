using Flit.Identity.Shared.Auth;
using Flit.Procedures.Runtime.Auth;
using Flit.Procedures.Runtime.Create;
using Flit.Procedures.Runtime.Dashboard;
using Flit.Procedures.Runtime.Documents;
using Flit.Procedures.Runtime.Index;
using Flit.Procedures.Runtime.Lookups;
using Flit.Procedures.Runtime.Ot;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Procedures.Runtime.Endpoints;

public static class TramitesEndpoints
{
    public static IEndpointRouteBuilder MapTramitesEndpoints(this IEndpointRouteBuilder app)
    {
        var readGroup = app.MapGroup("/api/v1/tramites").RequireTramitesRead();
        readGroup.MapGet("/index", GetIndexAsync);

        var createGroup = app.MapGroup("/api/v1/tramites").RequireTramitesCreate();
        createGroup.MapGet("/procedure-types", ListProcedureTypesAsync);
        createGroup.MapGet("/procedure-types/{id:guid}", GetProcedureTypeByIdAsync);
        createGroup.MapGet("/traffic-authorities", ListTrafficAuthoritiesAsync);
        createGroup.MapGet("/lookups/rues", LookupRuesAsync);
        createGroup.MapGet("/lookups/simit", LookupSimitAsync);
        createGroup.MapGet("/lookups/rnmc", LookupRnmcAsync);
        createGroup.MapPost("/", CreateAsync);
        createGroup.MapPost("/{id:guid}/documents", UploadDocumentAsync)
            .DisableAntiforgery();

        var dashboardGroup = app.MapGroup("/api/v1/tramites/dashboard").RequireDashboardAccess();
        dashboardGroup.MapGet("/summary", GetDashboardSummaryAsync);
        dashboardGroup.MapGet("/detail", GetDashboardDetailAsync);
        dashboardGroup.MapGet("/users/top", GetDashboardUsersTopAsync);
        dashboardGroup.MapGet("/users", GetDashboardUsersSearchAsync);
        dashboardGroup.MapGet("/users/{userId:guid}/stats", GetDashboardUserStatsAsync);
        dashboardGroup.MapGet("/export", GetDashboardExportAsync);

        return app;
    }

    private static Task<IResult> GetIndexAsync(
        HttpContext http,
        TramitesIndexHandler handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20) =>
        handler.HandleAsync((CurrentUser)http.Items["CurrentUser"]!, page, pageSize, ct);

    private static async Task<IResult> ListProcedureTypesAsync(
        IProcedureDefinitionService definitions,
        CancellationToken ct)
    {
        var items = await definitions.ListActiveAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetProcedureTypeByIdAsync(
        Guid id,
        IProcedureDefinitionService definitions,
        CancellationToken ct)
    {
        var definition = await definitions.GetByIdAsync(id, ct);
        return definition is null
            ? Results.Json(new { code = "NOT_FOUND" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(definition);
    }

    private static async Task<IResult> ListTrafficAuthoritiesAsync(
        HttpContext http,
        TrafficAuthorityPicker picker,
        CancellationToken ct)
    {
        var user = (CurrentUser)http.Items["CurrentUser"]!;
        if (user.TenantId is not { } tenantId)
        {
            return TenantContextRequired();
        }

        var items = await picker.ListForTenantAsync(tenantId, ct);
        return Results.Ok(items);
    }

    private static Task<IResult> LookupRuesAsync(
        string nit,
        IExternalLookupService lookups,
        CancellationToken ct) =>
        string.IsNullOrWhiteSpace(nit)
            ? Task.FromResult(ValidationError("nit is required."))
            : ExecuteLookupAsync(lookups.QueryRuesAsync(nit.Trim(), ct));

    private static Task<IResult> LookupSimitAsync(
        DocumentIdType documentType,
        string documentNumber,
        IExternalLookupService lookups,
        CancellationToken ct) =>
        string.IsNullOrWhiteSpace(documentNumber)
            ? Task.FromResult(ValidationError("documentNumber is required."))
            : ExecuteLookupAsync(lookups.QuerySimitAsync(documentType, documentNumber.Trim(), ct));

    private static Task<IResult> LookupRnmcAsync(
        DocumentIdType documentType,
        string documentNumber,
        IExternalLookupService lookups,
        CancellationToken ct) =>
        string.IsNullOrWhiteSpace(documentNumber)
            ? Task.FromResult(ValidationError("documentNumber is required."))
            : ExecuteLookupAsync(lookups.QueryRnmcAsync(documentType, documentNumber.Trim(), ct));

    private static async Task<IResult> ExecuteLookupAsync(Task<object> lookupTask)
    {
        var data = await lookupTask;
        return Results.Ok(data);
    }

    private static Task<IResult> CreateAsync(
        CreateTramiteRequest request,
        HttpContext http,
        TramitesCreateHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(request, (CurrentUser)http.Items["CurrentUser"]!, ct);

    private static Task<IResult> UploadDocumentAsync(
        Guid id,
        string label,
        IFormFile file,
        HttpContext http,
        TramitesDocumentUploadHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, label, file, (CurrentUser)http.Items["CurrentUser"]!, ct);

    private static Task<IResult> GetDashboardSummaryAsync(
        HttpContext http,
        TramitesDashboardSummaryHandler handler,
        CancellationToken ct,
        string? from = null,
        string? to = null,
        Guid? tenantId = null) =>
        handler.HandleAsync((CurrentUser)http.Items["CurrentUser"]!, from, to, tenantId, ct);

    private static Task<IResult> GetDashboardDetailAsync(
        HttpContext http,
        TramitesDashboardDetailHandler handler,
        CancellationToken ct,
        string category,
        string? from = null,
        string? to = null,
        Guid? tenantId = null,
        int page = 1,
        int pageSize = 20) =>
        handler.HandleAsync(
            (CurrentUser)http.Items["CurrentUser"]!,
            category,
            from,
            to,
            tenantId,
            page,
            pageSize,
            ct);

    private static Task<IResult> GetDashboardUsersTopAsync(
        HttpContext http,
        TramitesDashboardUsersHandler handler,
        CancellationToken ct,
        string? from = null,
        string? to = null,
        Guid? tenantId = null) =>
        handler.HandleTopAsync((CurrentUser)http.Items["CurrentUser"]!, from, to, tenantId, ct);

    private static Task<IResult> GetDashboardUsersSearchAsync(
        HttpContext http,
        TramitesDashboardUsersHandler handler,
        CancellationToken ct,
        string? q = null,
        Guid? tenantId = null,
        int page = 1,
        int pageSize = 20) =>
        handler.HandleSearchAsync((CurrentUser)http.Items["CurrentUser"]!, q, tenantId, page, pageSize, ct);

    private static Task<IResult> GetDashboardUserStatsAsync(
        HttpContext http,
        TramitesDashboardUsersHandler handler,
        Guid userId,
        CancellationToken ct,
        string? from = null,
        string? to = null,
        Guid? tenantId = null) =>
        handler.HandleStatsAsync((CurrentUser)http.Items["CurrentUser"]!, userId, from, to, tenantId, ct);

    private static Task<IResult> GetDashboardExportAsync(
        HttpContext http,
        TramitesDashboardExportHandler handler,
        CancellationToken ct,
        string category,
        string? from = null,
        string? to = null,
        Guid? tenantId = null) =>
        handler.HandleAsync((CurrentUser)http.Items["CurrentUser"]!, category, from, to, tenantId, ct);

    private static IResult ValidationError(string message) =>
        Results.Json(new { code = "VALIDATION_ERROR", message }, statusCode: StatusCodes.Status400BadRequest);

    private static IResult TenantContextRequired() =>
        Results.Json(
            new
            {
                code = "FORBIDDEN",
                message = "Los trámites requieren un usuario con tenant asignado. Inicie sesión como administrador de compañía.",
            },
            statusCode: StatusCodes.Status403Forbidden);
}
