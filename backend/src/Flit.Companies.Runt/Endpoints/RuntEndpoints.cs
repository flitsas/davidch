using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Companies.Runt.Endpoints;

public static class RuntEndpoints
{
    public static IEndpointRouteBuilder MapRuntEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/runt/{type}", QueryRuntAsync)
            .RequireAuthorization();

        return app;
    }

    private static Task<IResult> QueryRuntAsync(
        string type,
        string? q,
        ClaimsPrincipal user,
        RuntQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(user, type, q ?? "", ct);
}
