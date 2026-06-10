using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Identity.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", (LoginRequest req, LoginHandler handler, HttpContext ctx, CancellationToken ct) =>
            handler.HandleAsync(req, ctx, ct));

        group.MapPost("/refresh", (RefreshHandler handler, HttpContext ctx, CancellationToken ct) =>
            handler.HandleAsync(ctx, ct));

        group.MapPost("/logout", (LogoutHandler handler, HttpContext ctx, CancellationToken ct) =>
            handler.HandleAsync(ctx, ct));

        group.MapGet("/me", (MeHandler handler, HttpContext ctx, CancellationToken ct) =>
            handler.HandleAsync(ctx, ct));

        return app;
    }
}
