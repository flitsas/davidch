using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public record ActivateRequest(string Token, string Password);

public sealed class ActivateHandler(IdentityDbContext db, IPasswordHasher hasher)
{
    public async Task<IResult> HandleAsync(ActivateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.Password))
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Token and password are required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var hash = TokenHasher.Sha256(req.Token);
        var invitation = await db.InvitationTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (invitation is null
            || invitation.UsedAt is not null
            || invitation.ExpiresAt < DateTimeOffset.UtcNow
            || invitation.User.Status != UserStatus.Pending)
        {
            return Results.Json(
                new { code = ApiErrorCodes.InvitationTokenInvalid },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = invitation.User;
        user.PasswordHash = hasher.Hash(req.Password);
        user.Status = UserStatus.Active;
        user.ActivatedAt = DateTimeOffset.UtcNow;
        invitation.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { email = user.Email });
    }
}
