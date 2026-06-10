using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Identity.Auth;

public sealed class MeHandler(IdentityDbContext db, RsaJwtService jwt)
{
    public async Task<IResult> HandleAsync(HttpContext http, CancellationToken ct)
    {
        ClaimsPrincipal principal;
        try
        {
            if (!http.Request.Cookies.TryGetValue(AuthCookieOptions.AccessCookieName, out var token)
                || string.IsNullOrWhiteSpace(token))
            {
                return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
            }

            principal = jwt.Validate(token);
        }
        catch (SecurityTokenExpiredException)
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (SecurityTokenException)
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null)
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var claimVersion = int.Parse(principal.FindFirstValue("token_version") ?? "0");
        if (claimVersion != user.TokenVersion)
        {
            http.Response.Headers["X-Session-Revoked"] = "true";
            return Results.Json(
                new { code = ApiErrorCodes.SessionRevoked, message = "Session revoked." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var roles = principal.Claims
            .Where(c => c.Type is "roles" or ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToList();
        var permissions = principal.FindAll("permissions")
            .Select(c =>
            {
                var parts = c.Value.Split('|', 2);
                return new MePermission(parts[0], Enum.Parse<PermissionScope>(parts[1]));
            })
            .ToList();

        var isSuperAdmin = principal.FindFirstValue("is_super_admin") == "true";
        Guid? tenantId = principal.FindFirstValue("tenant_id") is { } tid ? Guid.Parse(tid) : null;

        return Results.Ok(new MeResponse(
            user.Id,
            user.Email,
            tenantId,
            roles,
            permissions,
            isSuperAdmin));
    }
}

public record MePermission(string Key, PermissionScope Scope);

public record MeResponse(
    Guid Id,
    string Email,
    Guid? TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<MePermission> Permissions,
    bool IsSuperAdmin);
