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

public record LoginRequest(string Email, string Password);

public sealed class LoginHandler(
    IdentityDbContext db,
    IPasswordHasher hasher,
    PermissionResolver resolver,
    RsaJwtService jwt,
    IWebHostEnvironment env)
{
    public async Task<IResult> HandleAsync(LoginRequest req, HttpContext http, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);

        if (user is null || user.Status != UserStatus.Active || user.PasswordHash is null
            || !hasher.Verify(req.Password, user.PasswordHash))
        {
            return Results.Json(new { code = ApiErrorCodes.InvalidCredentials }, statusCode: StatusCodes.Status401Unauthorized);
        }

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

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(refreshRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);

        http.Response.Cookies.Append(AuthCookieOptions.AccessCookieName, access, AuthCookieOptions.Access(15, env));
        http.Response.Cookies.Append(AuthCookieOptions.RefreshCookieName, refreshRaw, AuthCookieOptions.Refresh(7, env));

        return Results.Ok(new { id = user.Id, email = user.Email, tenant_id = user.TenantId, roles });
    }
}
