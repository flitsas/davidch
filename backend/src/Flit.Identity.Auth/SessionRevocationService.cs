using Flit.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public sealed class SessionRevocationService(IdentityDbContext db)
{
    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        user.TokenVersion += 1;

        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
