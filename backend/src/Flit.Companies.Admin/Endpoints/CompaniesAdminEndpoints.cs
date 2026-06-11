using Flit.Companies.Admin.Auth;
using Flit.Companies.Admin.Index;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Companies.Admin.Endpoints;

public static class CompaniesAdminEndpoints
{
    public static IEndpointRouteBuilder MapCompaniesAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/companies")
            .RequireSuperAdmin();

        group.MapGet("/index", GetIndexAsync);

        return app;
    }

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
