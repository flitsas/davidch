using Flit.Identity.Infrastructure.Tenancy;
using Flit.Identity.Shared.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenant = context.RequestServices.GetRequiredService<ITenantContext>();
        var userId = context.User.GetUserId();
        if (userId is not null)
        {
            tenant.UserId = userId.Value;
            tenant.IsSuperAdmin = context.User.FindFirst("is_super_admin")?.Value == "true";
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            tenant.CurrentTenantId = tenantClaim is null ? null : Guid.Parse(tenantClaim);
            tenant.TokenVersion = int.Parse(context.User.FindFirst("token_version")!.Value);
        }

        await next(context);
    }
}
