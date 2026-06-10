using Flit.Identity.Infrastructure.Audit;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.Rbac;

public sealed class RequirePermissionFilter(
    string permissionKey,
    PermissionScope scope,
    AuthorizationService authorization,
    AuditService audit) : IEndpointFilter
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

        if (user.IsSuperAdmin)
        {
            await audit.LogSuperAdminBypassAsync(
                user.Id,
                permissionKey,
                http.Request.Path.Value,
                http.RequestAborted);
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
            return async invocationContext =>
            {
                var services = invocationContext.HttpContext.RequestServices;
                var filter = new RequirePermissionFilter(
                    permissionKey,
                    scope,
                    services.GetRequiredService<AuthorizationService>(),
                    services.GetRequiredService<AuditService>());
                return await filter.InvokeAsync(invocationContext, next);
            };
        });
    }
}
