using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public sealed class LogoutHandler(IdentityDbContext db, IWebHostEnvironment env)
{
    public async Task<IResult> HandleAsync(HttpContext http, CancellationToken ct)
    {
        if (http.Request.Cookies.TryGetValue(AuthCookieOptions.RefreshCookieName, out var refreshRaw)
            && !string.IsNullOrWhiteSpace(refreshRaw))
        {
            var tokenHash = TokenHasher.Sha256(refreshRaw);
            var stored = await db.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null, ct);

            if (stored is not null)
            {
                stored.RevokedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        http.Response.Cookies.Delete(AuthCookieOptions.AccessCookieName, AuthCookieOptions.Delete(env));
        http.Response.Cookies.Delete(AuthCookieOptions.RefreshCookieName, AuthCookieOptions.Delete(env));

        return Results.NoContent();
    }
}
