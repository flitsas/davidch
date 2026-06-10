using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Auth;

public record ResetPasswordRequest(string Token, string NewPassword);

public sealed class ResetPasswordHandler(
    IdentityDbContext db,
    IPasswordHasher hasher,
    SessionRevocationService revocation)
{
    public async Task<IResult> HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Token and new password are required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var hash = TokenHasher.Sha256(req.Token);
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (resetToken is null
            || resetToken.UsedAt is not null
            || resetToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ResetTokenInvalid },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = resetToken.User;
        user.PasswordHash = hasher.Hash(req.NewPassword);
        resetToken.UsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await revocation.RevokeAllSessionsAsync(user.Id, ct);

        return Results.Ok(new { email = user.Email });
    }
}
