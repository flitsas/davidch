using Flit.Identity.Rbac;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.Companies.Admin.Auth;

public sealed class RequireSuperAdminFilter : IEndpointFilter
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

        if (!user.IsSuperAdmin)
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden));
        }

        http.Items["CurrentUser"] = user;
        return next(context);
    }
}

public static class RequireSuperAdminExtensions
{
    public static RouteGroupBuilder RequireSuperAdmin(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<RequireSuperAdminFilter>();
        return group;
    }

    public static RouteHandlerBuilder RequireSuperAdmin(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<RequireSuperAdminFilter>();
}
