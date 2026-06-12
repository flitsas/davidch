using Flit.Identity.Rbac;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Procedures.Runtime.Auth;

public sealed class RequireDashboardAccessFilter : IEndpointFilter
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

        if (user.IsSuperAdmin)
        {
            http.Items["CurrentUser"] = user;
            return next(context);
        }

        if (user.TenantId is not { } tenantId)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        var authorization = http.RequestServices.GetRequiredService<AuthorizationService>();
        var resource = new ResourceContext(tenantId, user.Id);
        if (!authorization.CanAccess(user, "users:read", resource))
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        http.Items["CurrentUser"] = user;
        return next(context);
    }
}

public static class RequireDashboardAccessExtensions
{
    public static RouteGroupBuilder RequireDashboardAccess(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(new RequireDashboardAccessFilter());
        return group;
    }
}
