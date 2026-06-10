using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flit.Identity.Infrastructure.Tenancy;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Identity.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next, ITenantContext tenant)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is not null)
        {
            tenant.UserId = Guid.Parse(sub);
            tenant.IsSuperAdmin = context.User.FindFirst("is_super_admin")?.Value == "true";
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            tenant.CurrentTenantId = tenantClaim is null ? null : Guid.Parse(tenantClaim);
            tenant.TokenVersion = int.Parse(context.User.FindFirst("token_version")!.Value);
        }

        await next(context);
    }
}
