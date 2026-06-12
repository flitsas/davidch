using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Procedures.Runtime.Auth;

public sealed class RequireTramitesPermissionFilter(string permissionKey) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var user = CurrentUserFactory.FromPrincipal(http.User);
        if (user is null)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.TokenExpired },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        if (user.TenantId is not { } tenantId && !user.IsSuperAdmin)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        var authorization = http.RequestServices.GetRequiredService<AuthorizationService>();
        var resourceTenantId = user.TenantId ?? Guid.Empty;
        var resource = new ResourceContext(resourceTenantId, user.Id);
        if (!authorization.CanAccess(user, permissionKey, resource))
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        http.Items["CurrentUser"] = user;
        return next(context);
    }
}

public static class RequireTramitesPermissionExtensions
{
    public static RouteGroupBuilder RequireTramitesRead(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(new RequireTramitesPermissionFilter("tramites:read"));
        return group;
    }

    public static RouteGroupBuilder RequireTramitesCreate(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(new RequireTramitesPermissionFilter("tramites:create"));
        return group;
    }
}
