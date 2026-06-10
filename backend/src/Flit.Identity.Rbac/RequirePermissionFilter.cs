using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.Rbac;

public sealed class RequirePermissionFilter(
    string permissionKey,
    PermissionScope scope,
    AuthorizationService authorization) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var user = CurrentUserFactory.FromPrincipal(http.User);
        if (user is null)
        {
            return Results.Json(
                new { code = ApiErrorCodes.TokenExpired },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var resource = user.TenantId is { } tenantId
            ? new ResourceContext(tenantId, user.Id)
            : null;

        if (!authorization.CanAccess(user, permissionKey, resource))
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden);
        }

        http.Items["CurrentUser"] = user;
        return await next(context);
    }
}

public static class RequirePermissionExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionKey,
        PermissionScope scope)
    {
        return builder.AddEndpointFilterFactory((factoryContext, next) =>
        {
            var filter = new RequirePermissionFilter(
                permissionKey,
                scope,
                factoryContext.ApplicationServices.GetRequiredService<AuthorizationService>());
            return invocationContext => filter.InvokeAsync(invocationContext, next);
        });
    }
}
