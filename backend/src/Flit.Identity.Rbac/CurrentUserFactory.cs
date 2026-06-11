using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Rbac;

public static class CurrentUserFactory
{
    public static CurrentUser? FromPrincipal(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null)
        {
            return null;
        }

        var permissions = principal.FindAll("permissions")
            .Select(c =>
            {
                var parts = c.Value.Split('|', 2);
                return new PermissionGrant(parts[0], Enum.Parse<PermissionScope>(parts[1]));
            })
            .ToList();

        return new CurrentUser(
            Guid.Parse(sub),
            principal.FindFirstValue("tenant_id") is { } tid ? Guid.Parse(tid) : null,
            principal.FindFirstValue("is_super_admin") == "true",
            int.Parse(principal.FindFirstValue("token_version") ?? "0"),
            permissions);
    }
}
