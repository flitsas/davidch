using Flit.OT.Admin.Auth;
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

        return app;
    }

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
