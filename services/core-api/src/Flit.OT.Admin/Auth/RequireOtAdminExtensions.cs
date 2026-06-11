using Flit.Identity.Rbac;
using Flit.Identity.Shared.Errors;
using Flit.OT.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.OT.Admin.Auth;

public sealed class RequireOtAdminFilter : IEndpointFilter
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

        if (user.TenantId is not { } tenantId)
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var authorization = http.RequestServices.GetRequiredService<AuthorizationService>();
        var resource = new ResourceContext(tenantId, user.Id);
        if (!authorization.CanAccess(user, "tramites:update", resource))
        {
            return Results.Json(
                new { code = ApiErrorCodes.Forbidden },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var otDb = http.RequestServices.GetRequiredService<OtDbContext>();
        var hasProfile = await otDb.OtProfiles.AsNoTracking()
            .AnyAsync(o => o.TenantId == tenantId, http.RequestAborted);

        if (!hasProfile)
        {
            return Results.Json(
                new { code = ApiErrorCodes.NotFound },
                statusCode: StatusCodes.Status404NotFound);
        }

        http.Items["CurrentUser"] = user;
        return await next(context);
    }
}

public static class RequireOtAdminExtensions
{
    public static RouteGroupBuilder RequireOtAdmin(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<RequireOtAdminFilter>();
        return group;
    }
}
