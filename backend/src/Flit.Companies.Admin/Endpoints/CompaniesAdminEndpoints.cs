using Flit.Companies.Admin.Auth;
using Flit.Companies.Admin.Config;
using Flit.Companies.Admin.Crud;
using Flit.Companies.Admin.Index;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Flit.Companies.Admin.Endpoints;

public static class CompaniesAdminEndpoints
{
    public static IEndpointRouteBuilder MapCompaniesAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/companies")
            .RequireSuperAdmin();

        group.MapGet("/index", GetIndexAsync);
        group.MapPost("/", CreateCompanyAsync);
        group.MapGet("/{id:guid}", GetCompanyAsync);

        group.MapGet("/{id:guid}/config/matricula", GetMatriculaAsync);
        group.MapPut("/{id:guid}/config/matricula", PutMatriculaAsync);
        group.MapGet("/{id:guid}/config/traspasos", GetTraspasosAsync);
        group.MapPut("/{id:guid}/config/traspasos", PutTraspasosAsync);
        group.MapGet("/{id:guid}/config/signatures", GetSignaturesAsync);
        group.MapPut("/{id:guid}/config/signatures", PutSignaturesAsync);
        group.MapGet("/{id:guid}/config/notifications", GetNotificationsAsync);
        group.MapPut("/{id:guid}/config/notifications", PutNotificationsAsync);
        group.MapGet("/{id:guid}/config/runt", GetRuntConfigAsync);
        group.MapPut("/{id:guid}/config/runt", PutRuntConfigAsync);


        group.MapGet("/{id:guid}/traffic-authorities", GetTrafficAuthoritiesAsync);
        group.MapPatch("/{id:guid}/traffic-authorities", PatchTrafficAuthoritiesAsync);

        return app;
    }

    private static Task<IResult> CreateCompanyAsync(
        CreateCompanyRequest request,
        CompanyCrudHandler handler,
        CancellationToken ct) =>
        handler.CreateAsync(request, ct);

    private static Task<IResult> GetCompanyAsync(
        Guid id,
        CompanyCrudHandler handler,
        CancellationToken ct) =>
        handler.GetDetailAsync(id, ct);

    private static Task<IResult> UpdateCompanyAsync(
        Guid id,
        UpdateCompanyRequest request,
        CompanyCrudHandler handler,
        CancellationToken ct) =>
        handler.UpdateAsync(id, request, ct);

    private static Task<IResult> UpdateCompanyStatusAsync(
        Guid id,
        UpdateCompanyStatusRequest request,
        CompanyCrudHandler handler,
        CancellationToken ct) =>
        handler.UpdateStatusAsync(id, request, ct);

    private static Task<IResult> GetMatriculaAsync(Guid id, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.GetMatriculaAsync(id, ct);

    private static Task<IResult> PutMatriculaAsync(
        Guid id, MatriculaConfigDto body, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.PutMatriculaAsync(id, body, ct);

    private static Task<IResult> GetTraspasosAsync(Guid id, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.GetTraspasosAsync(id, ct);

    private static Task<IResult> PutTraspasosAsync(
        Guid id, TraspasoConfigDto body, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.PutTraspasosAsync(id, body, ct);

    private static Task<IResult> GetSignaturesAsync(Guid id, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.GetSignaturesAsync(id, ct);

    private static Task<IResult> PutSignaturesAsync(
        Guid id, SignatureConfigDto body, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.PutSignaturesAsync(id, body, ct);

    private static Task<IResult> GetNotificationsAsync(Guid id, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.GetNotificationsAsync(id, ct);

    private static Task<IResult> PutNotificationsAsync(
        Guid id, NotificationConfigDto body, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.PutNotificationsAsync(id, body, ct);

    private static Task<IResult> GetRuntConfigAsync(Guid id, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.GetRuntAsync(id, ct);

    private static Task<IResult> PutRuntConfigAsync(
        Guid id, RuntConfigDto body, CompanyConfigHandler handler, CancellationToken ct) =>
        handler.PutRuntAsync(id, body, ct);

    private static Task<IResult> GetTrafficAuthoritiesAsync(
        Guid id,
        CompanyTrafficAuthoritiesHandler handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20) =>
        handler.GetAsync(id, page, pageSize, ct);

    private static Task<IResult> PatchTrafficAuthoritiesAsync(
        Guid id,
        TrafficAuthoritiesPatchRequest body,
        CompanyTrafficAuthoritiesHandler handler,
        CancellationToken ct) =>
        handler.PatchAsync(id, body, ct);

    private static Task<IResult> GetIndexAsync(
        CompanyIndexHandler handler,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? nit = null,
        string? name = null,
        Guid? id = null,
        DateTimeOffset? auditFrom = null,
        DateTimeOffset? auditTo = null) =>
        handler.HandleAsync(new CompanyIndexRequest
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Nit = nit,
            Name = name,
            Id = id,
            AuditFrom = auditFrom,
            AuditTo = auditTo
        }, ct);
}
