using System.Security.Cryptography;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public sealed class RefreshHandler(
    IdentityDbContext db,
    PermissionResolver resolver,
    RsaJwtService jwt,
    IWebHostEnvironment env)
{
    public async Task<IResult> HandleAsync(HttpContext http, CancellationToken ct)
    {
        if (!http.Request.Cookies.TryGetValue(AuthCookieOptions.RefreshCookieName, out var refreshRaw)
            || string.IsNullOrWhiteSpace(refreshRaw))
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var tokenHash = TokenHasher.Sha256(refreshRaw);
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(
                rt => rt.TokenHash == tokenHash
                      && rt.RevokedAt == null
                      && rt.ExpiresAt > DateTimeOffset.UtcNow,
                ct);

        if (stored is null)
        {
            return Results.Json(new { code = ApiErrorCodes.TokenExpired }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = stored.User;
        if (user.Status != UserStatus.Active)
        {
            return Results.Json(new { code = ApiErrorCodes.SessionRevoked }, statusCode: StatusCodes.Status403Forbidden);
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;

        var newRefreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(newRefreshRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        });

        var permissions = await resolver.ResolveAsync(user, ct);
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var access = jwt.CreateAccessToken(
            user.Id,
            user.Email,
            user.TenantId,
            roles,
            permissions,
            user.TenantId is null,
            user.TokenVersion);

        await db.SaveChangesAsync(ct);

        http.Response.Cookies.Append(AuthCookieOptions.AccessCookieName, access, AuthCookieOptions.Access(15, env));
        http.Response.Cookies.Append(AuthCookieOptions.RefreshCookieName, newRefreshRaw, AuthCookieOptions.Refresh(7, env));

        return Results.Ok(new { id = user.Id, email = user.Email, tenant_id = user.TenantId, roles });
    }
}
